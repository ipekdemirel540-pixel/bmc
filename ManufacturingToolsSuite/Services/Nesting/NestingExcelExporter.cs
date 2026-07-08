using System.IO;
using ClosedXML.Excel;
using ManufacturingToolsSuite.Models.Nesting;

namespace ManufacturingToolsSuite.Services.Nesting;

public static class NestingExcelExporter
{
    public static void ExportFullReport(string outputPath, NestingPlan plan, string sourceName)
    {
        ExportProfiles(plan.Profiles, outputPath, sourceName);
    }

    public static void ExportProfiles(IReadOnlyList<CalculatedProfile> profiles, string outputPath, string sourceName)
    {
        using var workbook = new XLWorkbook();
        var grouped = profiles.GroupBy(x => x.Dimensions);

        var summarySheet = workbook.Worksheets.Add("Özet Raporu");
        string[] summaryHeaders = ["Profil Ölçüsü", "Toplam Profil Adedi", "Toplam Kullanım (m)", "Toplam Fire (mm)"];
        summarySheet.Cell(1, 1).InsertData(new[] { summaryHeaders });
        var summaryRow = 2;
        foreach (var group in grouped)
        {
            summarySheet.Cell(summaryRow, 1).Value = group.Key;
            summarySheet.Cell(summaryRow, 2).Value = group.Count();
            summarySheet.Cell(summaryRow, 3).Value = group.Sum(p => p.UsedLengthTotalMm) / 1000.0;
            summarySheet.Cell(summaryRow, 4).Value = group.Sum(p => p.RemainingLengthMm);
            summaryRow++;
        }
        StyleHeader(summarySheet.Range(1, 1, 1, summaryHeaders.Length));
        summarySheet.Columns().AdjustToContents();

        foreach (var group in grouped)
        {
            var sheetName = $"Detay_{SanitizeSheetName(group.Key)}";
            var sheet = workbook.Worksheets.Add(sheetName.Length > 31 ? sheetName[..31] : sheetName);

            var titleRange = sheet.Range("A1:F1").Merge();
            titleRange.Value = $"{group.Key} Ölçüsü Detayları";
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 16;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.Row(1).Height = 30;

            string[] headers = ["Profil No", "Kesilen Parçalar", "Kesim Ölçüleri (mm)", "Kullanım (mm)", "Fire (mm)", "Onay"];
            sheet.Cell(3, 1).InsertData(new[] { headers });
            StyleHeader(sheet.Range(3, 1, 3, headers.Length));

            var detailRow = 4;
            foreach (var profile in group.OrderBy(p => p.ProfileIndex))
            {
                sheet.Cell(detailRow, 1).Value = profile.ProfileIndex;
                sheet.Cell(detailRow, 2).Value = string.Join(" | ", profile.ComponentNumbers);
                sheet.Cell(detailRow, 3).Value = string.Join(" ; ", profile.UsedLengthsMm.Select(l => l.ToString("F0")));
                sheet.Cell(detailRow, 4).Value = profile.UsedLengthTotalMm;
                sheet.Cell(detailRow, 5).Value = profile.RemainingLengthMm;
                detailRow++;
            }

            sheet.Cell(detailRow, 1).Value = "TOPLAM:";
            sheet.Cell(detailRow, 2).Value = $"{group.Sum(p => p.ComponentNumbers.Count)} Adet";
            sheet.Cell(detailRow, 4).Value = group.Sum(p => p.UsedLengthTotalMm);
            sheet.Cell(detailRow, 5).Value = group.Sum(p => p.RemainingLengthMm);
            sheet.Range(detailRow, 1, detailRow, headers.Length).Style.Font.Bold = true;
            sheet.Range(detailRow, 1, detailRow, headers.Length).Style.Fill.BackgroundColor = XLColor.LightYellow;
            sheet.Columns().AdjustToContents();
        }

        workbook.SaveAs(outputPath);
    }

    public static void ExportDailyPlan(
        Dictionary<int, List<CalculatedProfile>> perDayProfiles,
        Dictionary<int, int> perDayVehicleCounts,
        string outputPath)
    {
        using var workbook = new XLWorkbook();
        var summarySheet = workbook.Worksheets.Add("Günlük Plan");
        string[] headers = ["Gün", "Araç Adedi", "Toplam Profil", "Kullanım (m)", "Fire (mm)"];
        summarySheet.Cell(1, 1).InsertData(new[] { headers });
        StyleHeader(summarySheet.Range(1, 1, 1, headers.Length));

        var row = 2;
        foreach (var kvp in perDayProfiles.OrderBy(k => k.Key))
        {
            var profiles = kvp.Value;
            summarySheet.Cell(row, 1).Value = kvp.Key;
            summarySheet.Cell(row, 2).Value = perDayVehicleCounts.GetValueOrDefault(kvp.Key);
            summarySheet.Cell(row, 3).Value = profiles.Count;
            summarySheet.Cell(row, 4).Value = profiles.Sum(p => p.UsedLengthTotalMm) / 1000.0;
            summarySheet.Cell(row, 5).Value = profiles.Sum(p => p.RemainingLengthMm);
            row++;
        }
        summarySheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static string SanitizeSheetName(string name) =>
        string.IsNullOrWhiteSpace(name) ? "Bilinmeyen" : name.Replace(" ", "_").Replace("*", "x");
}
