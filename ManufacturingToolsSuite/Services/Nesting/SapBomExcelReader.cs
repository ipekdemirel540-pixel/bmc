using System.Diagnostics;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ManufacturingToolsSuite.Models.Nesting;

namespace ManufacturingToolsSuite.Services.Nesting;

/// <summary>
/// Excel okuyucu: önce SAP BOM sabit kolonlar (B–F), sonra başlık tabanlı esnek format.
/// Apron Profiler (PROFİLLER + ÖZET) ve benzeri çok sayfalı dosyaları destekler.
/// </summary>
public static class SapBomExcelReader
{
    public enum ExcelFormat { SapBomFixedColumns, HeaderBasedFlexible }

    public static ExcelFormat? LastMatchedFormat { get; private set; }

    public static IReadOnlyList<NestingRow> Read(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var dimensionLookup = BuildDimensionLookup(workbook);
        var sheetDiagnostics = new List<string>();

        foreach (var sheet in workbook.Worksheets)
        {
            var usedRange = sheet.RangeUsed();
            if (usedRange == null)
            {
                sheetDiagnostics.Add($"• {sheet.Name}: boş sayfa");
                continue;
            }

            var sapRows = ReadSapBomFixed(sheet, usedRange);
            if (sapRows.Count > 0)
            {
                LastMatchedFormat = ExcelFormat.SapBomFixedColumns;
                Debug.WriteLine($"[SapBomExcelReader] {sheet.Name} — SAP BOM sabit kolon ({sapRows.Count} satır).");
                return sapRows;
            }

            var flexRows = ReadHeaderBased(sheet, usedRange, dimensionLookup);
            if (flexRows.Count > 0)
            {
                LastMatchedFormat = ExcelFormat.HeaderBasedFlexible;
                Debug.WriteLine($"[SapBomExcelReader] {sheet.Name} — Başlık tabanlı esnek format ({flexRows.Count} satır).");
                return flexRows;
            }

            sheetDiagnostics.Add(DescribeSheetAttempt(sheet, usedRange));
        }

        LastMatchedFormat = null;
        var preview = sheetDiagnostics.Count > 0
            ? "\n\nDosya özeti:\n" + string.Join("\n", sheetDiagnostics)
            : string.Empty;

        throw CreateFormatException(
            "Dosyada geçerli parça satırı bulunamadı.\n\n" +
            "Desteklenen formatlar:\n" +
            "1) SAP BOM (sabit kolonlar):\n" +
            "   B = Bileşen No, C = Ad, D = Adet, E = Ölçü, F = Boy (metre)\n" +
            "2) Başlıklı profil listesi (Apron Profiler PROFİLLER vb.):\n" +
            "   Boy/Uzunluk/Length, Adet/Miktar (yoksa 1), Parça/Bileşen, Ölçü sütunları\n" +
            "   Uzunluk mm veya metre olarak otomatik algılanır." +
            preview);
    }

    private static List<NestingRow> ReadSapBomFixed(IXLWorksheet sheet, IXLRange usedRange)
    {
        var data = new List<NestingRow>();

        foreach (var row in usedRange.RowsUsed().Skip(1))
        {
            if (!TryParseLengthMeters(row.Cell(6), out var lenM))
                continue;

            var qty = ParseQuantity(row.Cell(4));
            if (qty <= 0) continue;

            data.Add(new NestingRow
            {
                ComponentNumber = GetCellText(row.Cell(2)),
                ComponentName = GetCellText(row.Cell(3)),
                Quantity = qty,
                Dimensions = GetCellText(row.Cell(5)),
                LengthMeters = lenM
            });
        }

        return data;
    }

    private static List<NestingRow> ReadHeaderBased(
        IXLWorksheet sheet,
        IXLRange usedRange,
        IReadOnlyDictionary<string, string> dimensionLookup)
    {
        var headerRowIndex = FindHeaderRow(sheet, usedRange);
        if (headerRowIndex < 0)
            return [];

        var map = MapColumns(sheet, headerRowIndex, usedRange.LastColumn().ColumnNumber());
        if (!map.LengthCol.HasValue)
            return [];

        var rawLengths = new List<double>();
        var pending = new List<(int Row, string CompNo, string CompName, int Qty, string Dim, double LenRaw)>();

        var lastRow = usedRange.LastRow().RowNumber();
        for (var r = headerRowIndex + 1; r <= lastRow; r++)
        {
            if (IsRowEmpty(sheet, r, usedRange.LastColumn().ColumnNumber()))
                continue;

            var lenCell = sheet.Cell(r, map.LengthCol.Value);
            if (!TryParseNumber(lenCell, out var lenRaw) || lenRaw <= 0)
                continue;

            var qty = map.QtyCol.HasValue ? ParseQuantity(sheet.Cell(r, map.QtyCol.Value)) : 1;
            if (qty <= 0) continue;

            var compNo = map.ComponentCol.HasValue
                ? GetCellText(sheet.Cell(r, map.ComponentCol.Value))
                : map.RecipeCol.HasValue
                    ? GetCellText(sheet.Cell(r, map.RecipeCol.Value))
                    : $"P{r}";

            var compName = map.NameCol.HasValue ? GetCellText(sheet.Cell(r, map.NameCol.Value)) : string.Empty;

            var dim = map.DimensionCol.HasValue ? GetCellText(sheet.Cell(r, map.DimensionCol.Value)) : string.Empty;
            if (string.IsNullOrWhiteSpace(dim) && map.RecipeCol.HasValue)
            {
                var recipeKey = GetCellText(sheet.Cell(r, map.RecipeCol.Value));
                if (!string.IsNullOrWhiteSpace(recipeKey) && dimensionLookup.TryGetValue(recipeKey, out var lookedUp))
                    dim = lookedUp;
            }

            if (string.IsNullOrWhiteSpace(dim) && !string.IsNullOrWhiteSpace(compName))
                dim = compName;

            if (string.IsNullOrWhiteSpace(compNo))
                compNo = $"P{r}";

            rawLengths.Add(lenRaw);
            pending.Add((r, compNo, compName, qty, dim, lenRaw));
        }

        if (pending.Count == 0)
            return [];

        var lengthsAreMm = DetectMillimeters(rawLengths);
        return pending.Select(p => new NestingRow
        {
            ComponentNumber = p.CompNo,
            ComponentName = p.CompName,
            Quantity = p.Qty,
            Dimensions = p.Dim,
            LengthMeters = lengthsAreMm ? p.LenRaw / 1000.0 : p.LenRaw
        }).ToList();
    }

    private static Dictionary<string, string> BuildDimensionLookup(XLWorkbook workbook)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in workbook.Worksheets)
        {
            var usedRange = sheet.RangeUsed();
            if (usedRange == null) continue;

            var headerRow = FindHeaderRow(sheet, usedRange);
            if (headerRow < 0) continue;

            var map = MapColumns(sheet, headerRow, usedRange.LastColumn().ColumnNumber());
            var recipeCol = map.RecipeCol ?? map.ComponentCol;
            if (!recipeCol.HasValue || !map.DimensionCol.HasValue)
                continue;

            var lastRow = usedRange.LastRow().RowNumber();
            for (var r = headerRow + 1; r <= lastRow; r++)
            {
                var key = GetCellText(sheet.Cell(r, recipeCol.Value));
                var dim = GetCellText(sheet.Cell(r, map.DimensionCol.Value));
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(dim))
                    lookup[key] = dim;
            }
        }

        return lookup;
    }

    private static string DescribeSheetAttempt(IXLWorksheet sheet, IXLRange usedRange)
    {
        var headerRow = FindHeaderRow(sheet, usedRange);
        var lastCol = Math.Min(usedRange.LastColumn().ColumnNumber(), 8);
        var headers = new List<string>();

        if (headerRow > 0)
        {
            for (var c = 1; c <= lastCol; c++)
            {
                var text = GetCellText(sheet.Cell(headerRow, c));
                if (!string.IsNullOrWhiteSpace(text))
                    headers.Add(text.Trim());
            }
        }
        else
        {
            for (var c = 1; c <= Math.Min(3, lastCol); c++)
            {
                var text = GetCellText(sheet.Cell(1, c));
                if (!string.IsNullOrWhiteSpace(text))
                    headers.Add(text.Trim());
            }
        }

        var headerPreview = headers.Count > 0
            ? string.Join(", ", headers.Take(3))
            : "(başlık bulunamadı)";

        return $"• {sheet.Name}: satır {usedRange.LastRow().RowNumber()}, başlıklar: {headerPreview}";
    }

    private static int FindHeaderRow(IXLWorksheet sheet, IXLRange usedRange)
    {
        var maxScan = Math.Min(usedRange.LastRow().RowNumber(), 30);
        var lastCol = usedRange.LastColumn().ColumnNumber();
        var bestRow = -1;
        var bestHits = 0;

        for (var r = 1; r <= maxScan; r++)
        {
            var hits = 0;
            for (var c = 1; c <= lastCol; c++)
            {
                var norm = NormalizeHeader(GetCellText(sheet.Cell(r, c)));
                if (IsLengthHeader(norm) || IsQtyHeader(norm) || IsComponentHeader(norm) ||
                    IsRecipeHeader(norm) || IsDimensionHeader(norm))
                    hits++;
            }

            if (hits > bestHits)
            {
                bestHits = hits;
                bestRow = r;
            }

            if (hits >= 2)
                return r;
        }

        return bestHits >= 2 ? bestRow : -1;
    }

    private static ColumnMap MapColumns(IXLWorksheet sheet, int headerRow, int lastCol)
    {
        var map = new ColumnMap();
        var lengthCandidates = new List<(int Col, int Score)>();
        var componentCandidates = new List<(int Col, int Score)>();
        var recipeCandidates = new List<(int Col, int Score)>();
        var qtyCandidates = new List<(int Col, int Score)>();
        var nameCandidates = new List<(int Col, int Score)>();
        var dimensionCandidates = new List<(int Col, int Score)>();

        for (var c = 1; c <= lastCol; c++)
        {
            var norm = NormalizeHeader(GetCellText(sheet.Cell(headerRow, c)));
            if (string.IsNullOrEmpty(norm)) continue;

            if (IsLengthHeader(norm))
                lengthCandidates.Add((c, ScoreLengthHeader(norm)));
            if (IsQtyHeader(norm))
                qtyCandidates.Add((c, ScoreQtyHeader(norm)));
            if (IsComponentHeader(norm))
                componentCandidates.Add((c, ScoreComponentHeader(norm)));
            if (IsRecipeHeader(norm))
                recipeCandidates.Add((c, ScoreRecipeHeader(norm)));
            if (IsNameHeader(norm))
                nameCandidates.Add((c, ScoreNameHeader(norm)));
            if (IsDimensionHeader(norm))
                dimensionCandidates.Add((c, ScoreDimensionHeader(norm)));
        }

        map.LengthCol = lengthCandidates.OrderByDescending(x => x.Score).Select(x => (int?)x.Col).FirstOrDefault();
        map.QtyCol = qtyCandidates.OrderByDescending(x => x.Score).Select(x => (int?)x.Col).FirstOrDefault();
        map.ComponentCol = componentCandidates.OrderByDescending(x => x.Score).Select(x => (int?)x.Col).FirstOrDefault();
        map.RecipeCol = recipeCandidates.OrderByDescending(x => x.Score).Select(x => (int?)x.Col).FirstOrDefault();
        map.NameCol = nameCandidates.OrderByDescending(x => x.Score).Select(x => (int?)x.Col).FirstOrDefault();
        map.DimensionCol = dimensionCandidates.OrderByDescending(x => x.Score).Select(x => (int?)x.Col).FirstOrDefault();

        return map;
    }

    private static bool IsRowEmpty(IXLWorksheet sheet, int row, int lastCol)
    {
        for (var c = 1; c <= lastCol; c++)
        {
            if (!string.IsNullOrWhiteSpace(GetCellText(sheet.Cell(row, c))))
                return false;
        }
        return true;
    }

    private static bool DetectMillimeters(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return false;
        var median = values.OrderBy(v => v).ElementAt(values.Count / 2);
        return median > 50;
    }

    private static bool HeaderContains(string h, params string[] keywords) =>
        keywords.Any(k => h.Contains(k, StringComparison.Ordinal));

    private static bool IsLengthHeader(string h) =>
        HeaderContains(h, "net_uzunluk", "uzunluk", "length", "len", "boy", "kesim", "miktar_mm", "miktari_mm") &&
        !HeaderContains(h, "ihtiyac", "fire", "recete", "arac");

    private static bool IsQtyHeader(string h) =>
        HeaderContains(h, "adet", "miktar", "qty", "quantity", "sayi", "arac_sayisi");

    private static bool IsComponentHeader(string h) =>
        HeaderContains(h, "profil_no", "profilno", "bilesenno", "componentno", "partno", "itemno") ||
        (HeaderContains(h, "parca", "bilesen", "component", "malzeme", "part", "item", "kod") &&
         !HeaderContains(h, "recete", "arac"));

    private static bool IsRecipeHeader(string h) =>
        HeaderContains(h, "parca_no", "parcano", "recete", "reçete") ||
        (h == "parca" || h == "bilesen");

    private static bool IsNameHeader(string h) =>
        HeaderContains(h, "aciklama", "description", "tanim", "isim", "name") &&
        !HeaderContains(h, "parca", "profil", "olcu", "kesit");

    private static bool IsDimensionHeader(string h) =>
        (HeaderContains(h, "kesit", "olcu", "dimension", "ebat", "olculer") ||
         HeaderContains(h, "kesit_olcu", "profil_olcu", "profilolcu")) &&
        !HeaderContains(h, "profil_no", "profilno", "recete", "ihtiyac", "fire");

    private static int ScoreLengthHeader(string h)
    {
        if (HeaderContains(h, "net_uzunluk")) return 100;
        if (HeaderContains(h, "uzunluk", "length")) return 80;
        if (HeaderContains(h, "boy")) return 60;
        return 10;
    }

    private static int ScoreQtyHeader(string h)
    {
        if (HeaderContains(h, "arac_sayisi", "arac")) return 100;
        if (HeaderContains(h, "adet")) return 80;
        return 10;
    }

    private static int ScoreComponentHeader(string h)
    {
        if (HeaderContains(h, "profil_no", "profilno")) return 100;
        if (HeaderContains(h, "bilesenno", "componentno")) return 90;
        return 10;
    }

    private static int ScoreRecipeHeader(string h)
    {
        if (HeaderContains(h, "parca_no", "parcano")) return 100;
        if (HeaderContains(h, "recete", "reçete")) return 90;
        return 10;
    }

    private static int ScoreNameHeader(string h) =>
        HeaderContains(h, "aciklama", "description") ? 100 : 10;

    private static int ScoreDimensionHeader(string h)
    {
        if (HeaderContains(h, "kesit")) return 100;
        if (HeaderContains(h, "olcu", "dimension")) return 80;
        return 10;
    }

    private static bool TryParseLengthMeters(IXLCell cell, out double meters)
    {
        meters = 0;
        if (!TryParseNumber(cell, out var value) || value <= 0)
            return false;
        meters = value > 50 ? value / 1000.0 : value;
        return true;
    }

    private static bool TryParseNumber(IXLCell cell, out double value)
    {
        value = 0;
        if (cell.TryGetValue(out double d))
        {
            value = d;
            return true;
        }
        var text = GetCellText(cell).Replace(',', '.');
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static int ParseQuantity(IXLCell cell)
    {
        if (cell.TryGetValue(out int i) && i > 0) return i;
        if (cell.TryGetValue(out double d) && d > 0) return (int)Math.Round(d);
        return int.TryParse(GetCellText(cell), out var q) && q > 0 ? q : 0;
    }

    private static string GetCellText(IXLCell cell)
    {
        if (cell.IsMerged())
        {
            var merged = cell.MergedRange();
            if (merged != null)
                return merged.FirstCell().GetString().Trim();
        }
        return cell.GetString().Trim();
    }

    private static string NormalizeHeader(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            sb.Append(ch switch
            {
                'ı' or 'i' or 'İ' or 'I' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' => 'u',
                'ö' or 'Ö' => 'o',
                'ç' or 'Ç' => 'c',
                ' ' or '-' or '_' or '.' or '/' or '(' or ')' => '_',
                _ when char.IsLetterOrDigit(ch) => ch,
                _ => '\0'
            });
        }
        return sb.ToString().Trim('_');
    }

    private static InvalidOperationException CreateFormatException(string message) =>
        new(message);

    private sealed class ColumnMap
    {
        public int? LengthCol { get; set; }
        public int? QtyCol { get; set; }
        public int? ComponentCol { get; set; }
        public int? RecipeCol { get; set; }
        public int? NameCol { get; set; }
        public int? DimensionCol { get; set; }
    }
}
