using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace ManufacturingToolsSuite.Services
{
    public class Settings
    {
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public WindowState WindowState { get; set; } = WindowState.Normal;
        public string LastModule { get; set; } = "Dashboard";
        public string? RecentModule { get; set; }
        public bool IsLightTheme { get; set; } = true;
        public string? LastNestingExcelPath { get; set; }
    }

    public static class UserSettingsService
    {
        private static string GetSettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ManufacturingToolsSuite");
            return Path.Combine(dir, "settings.json");
        }

        public static Settings Load()
        {
            try
            {
                var path = GetSettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var options = new JsonSerializerOptions
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    };
                    options.Converters.Add(new JsonStringEnumConverter());
                    var s = JsonSerializer.Deserialize<Settings>(json, options);
                    if (s != null)
                        return s;
                }
            }
            catch { /* ignore and return defaults */ }
            return new Settings();
        }

        public static void Save(Settings settings)
        {
            try
            {
                var path = GetSettingsPath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };
                options.Converters.Add(new JsonStringEnumConverter());
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(path, json);
            }
            catch { /* ignore */ }
        }
    }
}
