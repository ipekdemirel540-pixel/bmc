using System;
using System.Linq;
using System.Windows;

namespace ManufacturingToolsSuite.Services
{
    public static class ThemeService
    {
        private const string LightThemePath = "Resources/Brushes/LightTheme.xaml";
        private const string DarkThemePath = "Resources/Brushes/DarkTheme.xaml";

        public static bool IsLightTheme { get; private set; } = true;

        public static event Action? ThemeChanged;

        public static void ApplyTheme(bool isLight)
        {
            IsLightTheme = isLight;

            if (Application.Current?.Resources is not ResourceDictionary root)
                return;

            var dictionaries = root.MergedDictionaries;
            var current = dictionaries.FirstOrDefault(d =>
                d.Source?.OriginalString?.Contains("LightTheme.xaml", StringComparison.OrdinalIgnoreCase) == true ||
                d.Source?.OriginalString?.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) == true);

            if (current != null)
                dictionaries.Remove(current);

            var path = isLight ? LightThemePath : DarkThemePath;
            dictionaries.Insert(0, new ResourceDictionary
            {
                Source = new Uri(path, UriKind.Relative)
            });

            ThemeChanged?.Invoke();
        }
    }
}
