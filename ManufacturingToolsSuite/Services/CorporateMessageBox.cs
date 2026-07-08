using System.Windows;
using ManufacturingToolsSuite.Controls;

namespace ManufacturingToolsSuite.Services;

/// <summary>Corporate modal dialogs — drop-in replacement for MessageBox.Show.</summary>
public static class CorporateMessageBox
{
    public static MessageBoxResult Show(
        string message,
        string title = "Bilgi",
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.Information)
    {
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;

        var dialog = new Controls.CorporateDialog
        {
            Owner = owner,
            Topmost = owner == null
        };

        dialog.Configure(message, title, button, icon);
        dialog.ShowDialog();

        if (dialog.Result == MessageBoxResult.Cancel && button == MessageBoxButton.OK)
            return MessageBoxResult.OK;

        return dialog.Result;
    }

    public static MessageBoxResult ShowInfo(string message, string title = "Bilgi") =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public static MessageBoxResult ShowWarning(string message, string title = "Uyar\u0131") =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static MessageBoxResult ShowError(string message, string title = "Hata") =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
