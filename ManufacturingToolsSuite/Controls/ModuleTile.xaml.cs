using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ManufacturingToolsSuite.Services;

namespace ManufacturingToolsSuite.Controls
{
    public partial class ModuleTile : UserControl
    {
        public ModuleTile()
        {
            InitializeComponent();
            ThemeService.ThemeChanged += OnThemeChanged;
            Unloaded += (_, __) => ThemeService.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged() => UpdateIconBrush();

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ModuleTile), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty IconGlyphProperty =
            DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(ModuleTile), new PropertyMetadata(string.Empty));

        public string IconGlyph
        {
            get => (string)GetValue(IconGlyphProperty);
            set => SetValue(IconGlyphProperty, value);
        }

        public static readonly DependencyProperty GradientKeyProperty =
            DependencyProperty.Register(nameof(GradientKey), typeof(string), typeof(ModuleTile), new PropertyMetadata(string.Empty, OnGradientKeyChanged));

        public string GradientKey
        {
            get => (string)GetValue(GradientKeyProperty);
            set => SetValue(GradientKeyProperty, value);
        }

        private static void OnGradientKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ModuleTile tile)
            {
                tile.UpdateIconBrush();
            }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(ModuleTile), new PropertyMetadata(string.Empty));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ModuleTile), new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(nameof(CommandTarget), typeof(IInputElement), typeof(ModuleTile), new PropertyMetadata(null));

        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }

        public Brush IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }

        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register(nameof(IconBrush), typeof(Brush), typeof(ModuleTile), new PropertyMetadata(Brushes.White));

        private void UpdateIconBrush()
        {
            if (!string.IsNullOrWhiteSpace(GradientKey))
            {
                var brush = TryFindResource(GradientKey) as Brush;
                if (brush != null)
                {
                    IconBrush = brush;
                }
            }
        }
    }
}
