using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ManufacturingToolsSuite.Models.Nesting;
using ManufacturingToolsSuite.Services;
using ManufacturingToolsSuite.Services.Nesting;

namespace ManufacturingToolsSuite.Pages
{
    public partial class ExcelProcessorPage : Page
    {
        private string _excelPath = string.Empty;
        private List<DisplayRow> _allRows = new List<DisplayRow>();

        public ExcelProcessorPage()
        {
            InitializeComponent();
        }

        private void SelectExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel Dosyaları|*.xlsx;*.xls",
                Title = "SAP BOM Excel Dosyası Seçin"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadExcel(dlg.FileName);
            }
        }

        private void LoadExcel(string filePath)
        {
            try
            {
                var nestingRows = SapBomExcelReader.Read(filePath);
                
                _allRows = nestingRows.Select((row, idx) => new DisplayRow
                {
                    Index = idx + 1,
                    ComponentNumber = row.ComponentNumber,
                    ComponentName = row.ComponentName,
                    Quantity = row.Quantity,
                    Dimensions = row.Dimensions,
                    LengthMeters = row.LengthMeters
                }).ToList();

                // Stats calculation
                txtStatPartTypes.Text = _allRows.Count.ToString();
                txtStatTotalPieces.Text = _allRows.Sum(r => r.Quantity).ToString();
                txtStatDimensions.Text = _allRows.Select(r => r.Dimensions).Distinct().Count().ToString();

                var formatLabel = SapBomExcelReader.LastMatchedFormat switch
                {
                    SapBomExcelReader.ExcelFormat.SapBomFixedColumns => "SAP BOM (Sabit)",
                    SapBomExcelReader.ExcelFormat.HeaderBasedFlexible => "Başlıklı Liste",
                    _ => "Bilinmeyen"
                };

                _excelPath = filePath;
                lblFileName.Text = Path.GetFileName(filePath);
                lblFormatType.Text = formatLabel;

                // Bind DataGrid
                dgBomRows.ItemsSource = _allRows;

                // Toggle visibility
                pnlEmptyState.Visibility = Visibility.Collapsed;
                pnlDataState.Visibility = Visibility.Visible;
                cardFileLoadedActions.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                CorporateMessageBox.Show(
                    $"Excel okuma sırasında hata oluştu:\n\n{ex.Message}",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                dgBomRows.ItemsSource = _allRows;
            }
            else
            {
                var filtered = _allRows.Where(r =>
                    r.ComponentNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    r.ComponentName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    r.Dimensions.Contains(query, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                dgBomRows.ItemsSource = filtered;
            }
        }

        private void SendToNesting_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_excelPath) || !File.Exists(_excelPath))
            {
                CorporateMessageBox.Show("Yüklü dosya bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Navigate to NestingPage, passing the file path
            NavigationService?.Navigate(new NestingPage(_excelPath));
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_allRows.Count == 0) return;

            var dlg = new SaveFileDialog
            {
                Filter = "CSV Dosyası|*.csv",
                FileName = $"SAP_BOM_Export_{DateTime.Now:yyyyMMdd_HHmm}",
                Title = "BOM Listesini CSV Olarak Kaydet"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Index;ComponentNumber;ComponentName;Quantity;Dimensions;LengthMeters;LengthMm");
                    foreach (var r in _allRows)
                    {
                        sb.AppendLine($"{r.Index};{r.ComponentNumber};{r.ComponentName};{r.Quantity};{r.Dimensions};{r.LengthMeters.ToString("F3", CultureInfo.InvariantCulture)};{r.LengthMm.ToString("F0", CultureInfo.InvariantCulture)}");
                    }

                    File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

                    CorporateMessageBox.Show(
                        "CSV dosyası başarıyla kaydedildi.",
                        "Başarılı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CorporateMessageBox.Show(
                        $"Dosya kaydedilirken hata oluştu:\n\n{ex.Message}",
                        "Hata",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _excelPath = string.Empty;
            _allRows.Clear();
            dgBomRows.ItemsSource = null;
            txtSearch.Text = string.Empty;

            // Toggle visibility back
            pnlEmptyState.Visibility = Visibility.Visible;
            pnlDataState.Visibility = Visibility.Collapsed;
            cardFileLoadedActions.Visibility = Visibility.Collapsed;
        }
    }

    public class DisplayRow
    {
        public int Index { get; set; }
        public string ComponentNumber { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public double LengthMeters { get; set; }
        public double LengthMm => LengthMeters * 1000.0;
    }
}
