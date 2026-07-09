using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ManufacturingToolsSuite.Controls;

public partial class CorporateDialog : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

    public CorporateDialog()
    {
        InitializeComponent();
        Loaded += OnDialogLoaded;
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        if (Owner == null) return;

        try
        {
            if (Owner.IsVisible)
            {
                var screenPoint = Owner.PointToScreen(new Point(0, 0));
                var presentationSource = PresentationSource.FromVisual(Owner);
                if (presentationSource != null && presentationSource.CompositionTarget != null)
                {
                    var transform = presentationSource.CompositionTarget.TransformFromDevice;
                    var logicalPoint = transform.Transform(screenPoint);
                    Left = logicalPoint.X;
                    Top = logicalPoint.Y;
                }
                else
                {
                    if (!double.IsNaN(Owner.Left)) Left = Owner.Left;
                    if (!double.IsNaN(Owner.Top)) Top = Owner.Top;
                }
            }
            else
            {
                if (!double.IsNaN(Owner.Left)) Left = Owner.Left;
                if (!double.IsNaN(Owner.Top)) Top = Owner.Top;
            }
        }
        catch
        {
            if (!double.IsNaN(Owner.Left)) Left = Owner.Left;
            if (!double.IsNaN(Owner.Top)) Top = Owner.Top;
        }

        double ownerWidth = Owner.ActualWidth > 0 ? Owner.ActualWidth : Owner.Width;
        double ownerHeight = Owner.ActualHeight > 0 ? Owner.ActualHeight : Owner.Height;

        if (!double.IsNaN(ownerWidth) && ownerWidth > 0) Width = ownerWidth;
        if (!double.IsNaN(ownerHeight) && ownerHeight > 0) Height = ownerHeight;
    }

    public void Configure(string message, string title, MessageBoxButton button, MessageBoxImage icon)
    {
        TitleText.Text = title;
        MessageText.Text = message;
        ApplyIcon(icon);
        BuildButtons(button);
    }

    private void ApplyIcon(MessageBoxImage icon)
    {
        switch (icon)
        {
            case MessageBoxImage.Warning:
                IconGlyph.Foreground = (Brush)FindResource("WarningBrush");
                AccentBar.Background = (Brush)FindResource("WarningBrush");
                IconGlyph.Text = "\uE7BA";
                break;
            case MessageBoxImage.Error:
                IconGlyph.Foreground = (Brush)FindResource("ErrorBrush");
                AccentBar.Background = (Brush)FindResource("ErrorBrush");
                IconGlyph.Text = "\uE783";
                break;
            case MessageBoxImage.Question:
                IconGlyph.Foreground = (Brush)FindResource("AccentBrush");
                AccentBar.Background = (Brush)FindResource("AccentBrush");
                IconGlyph.Text = "\uE9CE";
                break;
            default:
                IconGlyph.Foreground = (Brush)FindResource("InfoBrush");
                AccentBar.Background = (Brush)FindResource("InfoBrush");
                IconGlyph.Text = "\uE946";
                break;
        }
    }

    private void BuildButtons(MessageBoxButton button)
    {
        ButtonPanel.Children.Clear();

        switch (button)
        {
            case MessageBoxButton.OK:
                AddButton("Tamam", MessageBoxResult.OK, primary: true);
                break;
            case MessageBoxButton.OKCancel:
                AddButton("\u0130ptal", MessageBoxResult.Cancel, primary: false);
                AddButton("Tamam", MessageBoxResult.OK, primary: true);
                break;
            case MessageBoxButton.YesNo:
                AddButton("Hay\u0131r", MessageBoxResult.No, primary: false);
                AddButton("Evet", MessageBoxResult.Yes, primary: true);
                break;
            case MessageBoxButton.YesNoCancel:
                AddButton("\u0130ptal", MessageBoxResult.Cancel, primary: false);
                AddButton("Hay\u0131r", MessageBoxResult.No, primary: false);
                AddButton("Evet", MessageBoxResult.Yes, primary: true);
                break;
        }
    }

    private void AddButton(string text, MessageBoxResult value, bool primary)
    {
        var btn = new Button
        {
            Content = text,
            MinWidth = primary ? 108 : 96,
            Height = 38,
            Margin = new Thickness(8, 0, 0, 0),
            FontFamily = (FontFamily)FindResource("SecondaryFont"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand,
            Background = primary
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("SurfaceBrush"),
            Foreground = primary
                ? Brushes.White
                : (Brush)FindResource("TextBrush"),
            BorderBrush = primary
                ? Brushes.Transparent
                : (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(primary ? 0 : 1),
            IsDefault = primary,
            IsCancel = value is MessageBoxResult.Cancel or MessageBoxResult.No
        };

        btn.Template = CreateRoundedButtonTemplate();
        btn.Click += (_, _) =>
        {
            Result = value;
            DialogResult = true;
            Close();
        };

        ButtonPanel.Children.Add(btn);
    }

    private static ControlTemplate CreateRoundedButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });
        border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });
        border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.MarginProperty, new Thickness(14, 8, 14, 8));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;
        return template;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); } catch { /* ignore */ }
        }
    }

    private void Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        DialogResult = false;
        Close();
    }
}
