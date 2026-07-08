using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ManufacturingToolsSuite.Controls;

public partial class SidebarNavItem : UserControl
{
    public SidebarNavItem()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateVisualState();
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SidebarNavItem), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SidebarNavItem), new PropertyMetadata(string.Empty));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(SidebarNavItem), new PropertyMetadata(string.Empty));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(SidebarNavItem),
            new PropertyMetadata(Brushes.Transparent));

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register(nameof(IconBackground), typeof(Brush), typeof(SidebarNavItem),
            new PropertyMetadata(Brushes.Transparent));

    public Brush IconBackground
    {
        get => (Brush)GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SidebarNavItem), new PropertyMetadata(null));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(SidebarNavItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(SidebarNavItem),
            new PropertyMetadata(false, OnIsCollapsedChanged));

    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarNavItem item) item.UpdateVisualState();
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarNavItem item) item.UpdateCollapsedState();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (Command?.CanExecute(null) == true)
            Command.Execute(null);
    }

    private void UpdateVisualState()
    {
        AccentBar.Visibility = IsSelected ? Visibility.Visible : Visibility.Collapsed;
        NavButton.Background = IsSelected
            ? (Brush)FindResource("SidebarNavActiveBrush")
            : Brushes.Transparent;
    }

    private void UpdateCollapsedState()
    {
        LabelPanel.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        NavButton.Padding = IsCollapsed ? new Thickness(0) : new Thickness(0, 2, 0, 2);
    }
}
