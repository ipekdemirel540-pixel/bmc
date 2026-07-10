using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ManufacturingToolsSuite.Models.Nesting;
using ManufacturingToolsSuite.Services;
using ManufacturingToolsSuite.Services.Nesting;
using Microsoft.Win32;

namespace ManufacturingToolsSuite.Pages;

public partial class NestingPage : Page
{
    private string _excelPath = string.Empty;
    private IReadOnlyList<NestingRow> _sourceRows = Array.Empty<NestingRow>();
    private NestingPlan? _currentPlan;
    private List<ProfileResultRow> _detailedResultRows = [];
    private List<ProfileResultRow> _mergedResultRows = [];
    private CancellationTokenSource? _cts;
    private Settings _settings = new();

    public NestingPage()
    {
        InitializeComponent();
        ApplyDefaults();
        LoadPersistedSettings();
        ShowEmptyState(true);
    }

    public NestingPage(string initialExcelPath) : this()
    {
        if (File.Exists(initialExcelPath))
        {
            _excelPath = initialExcelPath;
            _settings.LastNestingExcelPath = _excelPath;
            UserSettingsService.Save(_settings);
            txtFileName.Text = Path.GetFileName(_excelPath);
            LoadExcel(_excelPath, silent: true);
        }
    }

    private void ApplyDefaults()
    {
        txtMetricStandard.Text = $"{NestingParameters.DefaultStandardLengthMm} mm";
    }

    private void LoadPersistedSettings()
    {
        try
        {
            _settings = UserSettingsService.Load();
        }
        catch { /* ignore */ }
    }

    private void SelectExcel_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel Dosyalar\u0131|*.xlsx;*.xls",
            Title = "SAP BOM Excel Dosyas\u0131 Se\u00E7in"
        };
        if (dlg.ShowDialog() != true) return;

        _excelPath = dlg.FileName;
        _settings.LastNestingExcelPath = _excelPath;
        UserSettingsService.Save(_settings);
        txtFileName.Text = Path.GetFileName(_excelPath);
        LoadExcel(_excelPath, silent: false);
    }

    private void LoadExcel(string path, bool silent)
    {
        try
        {
            _sourceRows = SapBomExcelReader.Read(path);
            _currentPlan = null;
            ShowEmptyState(true);
            UpdateSummaryPreview();

            var formatLabel = SapBomExcelReader.LastMatchedFormat switch
            {
                SapBomExcelReader.ExcelFormat.SapBomFixedColumns => "SAP BOM",
                SapBomExcelReader.ExcelFormat.HeaderBasedFlexible => "Ba\u015Fl\u0131kl\u0131 liste",
                _ => "Bilinmeyen"
            };

            if (!silent)
            {
                SetStatus($"{_sourceRows.Count} par\u00E7a tipi y\u00FCklendi ({formatLabel})", NestingIcons.Success);
                CorporateMessageBox.Show(
                    $"{_sourceRows.Count} par\u00E7a tipi okundu.\nProfil gruplar\u0131: {_sourceRows.Select(r => r.Dimensions).Distinct().Count()}\nFormat: {formatLabel}",
                    "Veri Kayna\u011F\u0131",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                SetStatus($"{_sourceRows.Count} par\u00E7a y\u00FCkl\u00FC", NestingIcons.Success);
            }
        }
        catch (Exception ex)
        {
            _sourceRows = [];
            SetStatus("Excel okunamad\u0131", NestingIcons.Error);
            CorporateMessageBox.Show(ex.Message, "Excel Hatas\u0131", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Calculate_Click(object sender, RoutedEventArgs e)
    {
        await RunAsync("Kesim plan\u0131 hesaplan\u0131yor...", async ct =>
        {
            var parameters = BuildParameters();
            var validation = NestingValidator.Validate(_sourceRows, parameters);
            if (!validation.IsValid)
                throw new InvalidOperationException(string.Join("\n", validation.Errors));

            var plan = await Task.Run(() => NestingEngine.Optimize(_sourceRows, parameters), ct);
            ApplyPlan(plan);
            return "Hesaplama tamamland\u0131";
        });
    }

    private async void FindBestLength_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSourceData()) return;

        BestLengthResult? best = null;
        await RunAsync("En iyi boy analiz ediliyor...", async ct =>
        {
            var parameters = BuildParameters();
            best = await Task.Run(() => NestingEngine.FindBestLength(_sourceRows, parameters), ct);
            return $"En iyi boy: {best.StandardLengthMm} mm";
        });

        if (best == null) return;

        txtStandardLength.Text = best.StandardLengthMm.ToString(CultureInfo.InvariantCulture);
        txtMetricStandard.Text = $"{best.StandardLengthMm} mm";

        var usableMm = ProfileNestingCalculator.UsableLengthMm(best.StandardLengthMm, ParseInt(txtClamp.Text, NestingParameters.DefaultClampPerEndMm));
        var msg = $"\u00D6nerilen boy: {best.StandardLengthMm} mm\n" +
                  $"Kullan\u0131labilir: {usableMm} mm\n" +
                  $"Profil: {best.TotalProfiles}\n" +
                  $"Fire: {best.TotalWasteMm:N0} mm\n" +
                  $"Verim: {best.Utilization * 100:F2}%";

        CorporateMessageBox.Show(msg, "En \u0130yi Boy", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsurePlan()) return;
        var dlg = SaveDialog("NestingRaporu", "Excel Dosyas\u0131|*.xlsx");
        if (dlg == null) return;

        await RunAsync("Excel raporu olu\u015Fturuluyor...", async ct =>
        {
            await Task.Run(() => NestingExcelExporter.ExportFullReport(
                dlg.FileName, _currentPlan!, Path.GetFileName(_excelPath)), ct);
            OpenInExplorer(dlg.FileName);
            return "Excel raporu kaydedildi";
        });
    }

    private async void ExportDailyPlan_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSourceData()) return;
        var dlg = SaveDialog("GunlukPlan", "Excel Dosyas\u0131|*.xlsx");
        if (dlg == null) return;

        await RunAsync("G\u00FCnl\u00FCk plan haz\u0131rlan\u0131yor...", async ct =>
        {
            var parameters = BuildParameters();
            var perDayProfiles = new Dictionary<int, List<CalculatedProfile>>();
            var perDayCounts = new Dictionary<int, int>();

            await Task.Run(() =>
            {
                var remaining = parameters.TotalVehicles;
                var day = 1;
                while (remaining > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    var batch = Math.Min(parameters.VehiclesPerDay, remaining);
                    var dayPlan = NestingEngine.OptimizeDailyBatch(_sourceRows, parameters, batch);
                    perDayProfiles[day] = dayPlan.Profiles.ToList();
                    perDayCounts[day] = batch;
                    remaining -= batch;
                    day++;
                }
                NestingExcelExporter.ExportDailyPlan(perDayProfiles, perDayCounts, dlg.FileName);
            }, ct);

            OpenInExplorer(dlg.FileName);
            return "G\u00FCnl\u00FCk plan kaydedildi";
        });
    }

    private async void ExportSequence_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSourceData()) return;
        var sequence = SequenceParser.Parse(txtCustomSequence.Text);
        if (sequence.Count == 0)
        {
            CorporateMessageBox.Show("Ge\u00E7erli bir sekans girin. \u00D6rnek: 10-10-5-5", "Sekans", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (sequence.Sum() != ParseInt(txtVehicleCount.Text, 0))
        {
            CorporateMessageBox.Show("Sekans toplam\u0131 toplam ara\u00E7 say\u0131s\u0131na e\u015Fit olmal\u0131d\u0131r.", "Sekans", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = SaveDialog("SekansPlani", "Excel Dosyas\u0131|*.xlsx");
        if (dlg == null) return;

        await RunAsync("Sekans plan\u0131 olu\u015Fturuluyor...", async ct =>
        {
            var parameters = BuildParameters();
            var perDayProfiles = new Dictionary<int, List<CalculatedProfile>>();
            var perDayCounts = new Dictionary<int, int>();

            await Task.Run(() =>
            {
                var day = 1;
                foreach (var batch in sequence)
                {
                    ct.ThrowIfCancellationRequested();
                    var dayPlan = NestingEngine.OptimizeDailyBatch(_sourceRows, parameters, batch);
                    perDayProfiles[day] = dayPlan.Profiles.ToList();
                    perDayCounts[day] = batch;
                    day++;
                }
                NestingExcelExporter.ExportDailyPlan(perDayProfiles, perDayCounts, dlg.FileName);
            }, ct);

            OpenInExplorer(dlg.FileName);
            return "Sekans plan\u0131 kaydedildi";
        });
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void DimensionFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyDimensionFilter();

    private void IntegerOnly_Preview(object sender, TextCompositionEventArgs e) =>
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");

    private NestingParameters BuildParameters() => new()
    {
        TotalVehicles = ParseInt(txtVehicleCount.Text, 100),
        VehiclesPerDay = ParseInt(txtVehiclesPerDay.Text, 10),
        KerfMm = ParseInt(txtKerf.Text, 5),
        ClampPerEndMm = ParseInt(txtClamp.Text, 100),
        StandardLengthMm = ParseInt(txtStandardLength.Text, NestingParameters.DefaultStandardLengthMm),
        CustomSequence = SequenceParser.Parse(txtCustomSequence.Text),
        ExcelPath = _excelPath
    };

    private void ApplyPlan(NestingPlan plan)
    {
        _currentPlan = plan;

        _detailedResultRows = plan.Profiles.Select(p => new ProfileResultRow
        {
            ProfileIndex = p.ProfileIndex,
            Dimensions = p.Dimensions,
            Components = string.Join(" | ", p.ComponentNumbers),
            CutLengths = string.Join("; ", p.UsedLengthsMm.Select(l => l.ToString("F0"))),
            UsedMm = p.UsedLengthTotalMm,
            WasteMm = p.RemainingLengthMm,
            UtilizationPercent = p.UtilizationPercent,
            Quantity = 1
        }).ToList();

        // Calculate merged/grouped rows
        _mergedResultRows = _detailedResultRows
            .GroupBy(r => new { r.Dimensions, r.CutLengths })
            .Select((g, idx) =>
            {
                var first = g.First();
                return new ProfileResultRow
                {
                    ProfileIndex = idx + 1,
                    Dimensions = g.Key.Dimensions,
                    Components = string.Join(" | ", g.SelectMany(r => r.Components.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries)).Distinct()),
                    CutLengths = g.Key.CutLengths,
                    UsedMm = first.UsedMm,
                    WasteMm = first.WasteMm,
                    UtilizationPercent = first.UtilizationPercent,
                    Quantity = g.Count()
                };
            }).ToList();

        txtMetricProfiles.Text = plan.TotalProfileCount.ToString();
        txtMetricStandard.Text = $"{plan.Parameters.StandardLengthMm} mm";
        txtMetricWaste.Text = $"{plan.TotalWasteMm:N0} mm";
        txtMetricUtil.Text = $"{plan.OverallUtilizationPercent:0.#}%";

        cmbDimensionFilter.ItemsSource = new[] { "Tümü" }.Concat(plan.Profiles.Select(p => p.Dimensions).Distinct()).ToList();
        cmbDimensionFilter.SelectedIndex = 0;

        ShowEmptyState(false);
        ApplyDimensionFilter();
        UpdateSummaryPreview(plan);
    }

    private void ApplyDimensionFilter()
    {
        var sourceList = (chkMergeIdentical?.IsChecked == true) ? _mergedResultRows : _detailedResultRows;
        if (sourceList.Count == 0) return;

        var filter = cmbDimensionFilter.SelectedItem as string;
        dgResults.ItemsSource = filter is null or "T\u00FCm\u00FC"
            ? sourceList
            : sourceList.Where(r => r.Dimensions == filter).ToList();
    }

    private void ChkMergeIdentical_Click(object sender, RoutedEventArgs e)
    {
        if (colQuantity != null)
        {
            colQuantity.Visibility = (chkMergeIdentical.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
        }
        ApplyDimensionFilter();
    }

    private void UpdateSummaryPreview(NestingPlan? plan = null)
    {
        var parameters = BuildParameters();
        var analysis = NestingEngine.Analyze(_sourceRows, parameters);
        txtSummaryParts.Text = $"Par\u00E7a tipi: {analysis.PartTypeCount}";
        txtSummaryPieces.Text = $"Toplam adet: {analysis.TotalPieceCount}";
        txtSummaryGroups.Text = $"Profil grubu: {analysis.DimensionGroupCount}";
        txtSummaryDays.Text = plan != null
            ? $"\u00DCretim s\u00FCresi: {plan.DailyPlan.Count} g\u00FCn"
            : $"Tahmini s\u00FCre: ~{analysis.EstimatedDays} g\u00FCn";
    }

    private void ShowEmptyState(bool empty)
    {
        pnlEmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        dgResults.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private bool EnsureSourceData()
    {
        if (_sourceRows.Count > 0 && !string.IsNullOrEmpty(_excelPath)) return true;
        CorporateMessageBox.Show("\u00D6nce SAP BOM Excel dosyas\u0131 se\u00E7in.", "Veri Yok", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private bool EnsurePlan()
    {
        if (_currentPlan != null) return true;
        CorporateMessageBox.Show("\u00D6nce Hesapla ile kesim plan\u0131 olu\u015Fturun.", "Plan Yok", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private async Task RunAsync(string statusText, Func<CancellationToken, Task<string>> work)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        SetBusy(true);
        SetStatus(statusText, NestingIcons.Processing);

        try
        {
            var result = await work(token);
            SetStatus(result, NestingIcons.Success);
        }
        catch (OperationCanceledException)
        {
            SetStatus("\u0130\u015Flem iptal edildi", NestingIcons.Cancel);
        }
        catch (Exception ex)
        {
            SetStatus("Hata olu\u015Ftu", NestingIcons.Error);
            CorporateMessageBox.Show(ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        progressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnCancel.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnCalculate.IsEnabled = !busy;
    }

    private void SetStatus(string message, string icon)
    {
        var hasMessage = !string.IsNullOrWhiteSpace(message);
        txtStatus.Text = message;
        StatusIcon.Text = icon;
        StatusIcon.Visibility = hasMessage ? Visibility.Visible : Visibility.Collapsed;
    }

    private static int ParseInt(string? text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private static SaveFileDialog? SaveDialog(string prefix, string filter)
    {
        var dlg = new SaveFileDialog
        {
            Filter = filter,
            FileName = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmm}",
            Title = "Kaydet"
        };
        return dlg.ShowDialog() == true ? dlg : null;
    }

    private static void OpenInExplorer(string path) =>
        Process.Start("explorer.exe", $"/select,\"{path}\"");
}
