using System.Windows;
using System.Windows.Controls;

namespace ManufacturingToolsSuite.Pages;

public partial class BmcAiPage : Page
{
    public BmcAiPage()
    {
        InitializeComponent();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var text = PromptBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            OutputText.Text = "L\u00FCtfen bir istem girin.";
            return;
        }

        OutputText.Text = text;
        HistoryList.Items.Add(text);
        SummaryText.Text = $"Toplam \u00F6\u011Fe: {HistoryList.Items.Count}";
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is string s)
            OutputText.Text = s;
    }
}
