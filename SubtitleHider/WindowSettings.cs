using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SubtitleHider
{
    public class WindowSettingsData
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Opacity { get; set; }
    }

    public static class WindowSettings
    {
        private static readonly string SettingsPath;

        static WindowSettings()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "SubtitleHider");
            Directory.CreateDirectory(appFolder);
            SettingsPath = Path.Combine(appFolder, "settings.json");
        }

        public static WindowSettingsData Load()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<WindowSettingsData>(json) ?? GetDefault();
                }
                catch
                {
                    return GetDefault();
                }
            }
            return GetDefault();
        }

        public static void Save(double left, double top, double width, double height, double opacity)
        {
            var data = new WindowSettingsData
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                Opacity = opacity
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        private static WindowSettingsData GetDefault()
        {
            var screen = SystemParameters.WorkArea;
            return new WindowSettingsData
            {
                Left = (screen.Width - 1125) / 2,
                Top = screen.Height - 150,
                Width = 1125,
                Height = 75,
                Opacity = 1
            };
        }
    }
}
