using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OTPManager.Desktop.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public static class ThemeService
    {
        private const string SettingsFileName = "settings.json";
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OTPManager",
            SettingsFileName);

        private static AppTheme currentTheme = AppTheme.System;
        private static bool isInitialized = false;

        public static event Action<AppTheme, bool>? ThemeChanged; // (selectedTheme, isActuallyDark)

        public static AppTheme CurrentTheme => currentTheme;
        public static bool IsDark { get; private set; }

        public static void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            currentTheme = LoadSavedTheme();

            try
            {
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            }
            catch
            {
                // Ignore if UserPreferenceChanged fails
            }

            ApplyTheme(currentTheme, persist: false);
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && currentTheme == AppTheme.System)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ApplyTheme(AppTheme.System, persist: false);
                });
            }
        }

        public static void SetTheme(AppTheme theme)
        {
            currentTheme = theme;
            ApplyTheme(theme, persist: true);
        }

        public static void ApplyTheme(AppTheme theme, bool persist = true)
        {
            bool isDark = theme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                AppTheme.System => IsWindowsInDarkMode(),
                _ => false
            };

            IsDark = isDark;

            // Load theme ResourceDictionary
            var themeUri = new Uri(isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative);
            var newDict = new ResourceDictionary { Source = themeUri };

            var appResources = Application.Current?.Resources;
            if (appResources != null)
            {
                ResourceDictionary? oldDict = null;
                foreach (var dict in appResources.MergedDictionaries)
                {
                    if (dict.Source != null && (dict.Source.OriginalString.Contains("DarkTheme.xaml") || dict.Source.OriginalString.Contains("LightTheme.xaml")))
                    {
                        oldDict = dict;
                        break;
                    }
                }

                if (oldDict != null)
                {
                    var index = appResources.MergedDictionaries.IndexOf(oldDict);
                    appResources.MergedDictionaries[index] = newDict;
                }
                else
                {
                    appResources.MergedDictionaries.Add(newDict);
                }
            }

            // Update title bar for all open windows
            if (Application.Current != null)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    UpdateWindowTitleBar(window, isDark);
                }
            }

            if (persist)
            {
                SaveTheme(theme);
            }

            ThemeChanged?.Invoke(theme, isDark);
        }

        public static void RegisterWindow(Window window)
        {
            if (window == null) return;

            window.SourceInitialized += (s, e) =>
            {
                UpdateWindowTitleBar(window, IsDark);
            };

            if (window.IsLoaded)
            {
                UpdateWindowTitleBar(window, IsDark);
            }
        }

        public static void UpdateWindowTitleBar(Window window, bool isDark)
        {
            if (window == null) return;
            try
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle != IntPtr.Zero)
                {
                    int darkMode = isDark ? 1 : 0;
                    // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 10 20H1+ and Windows 11)
                    int hr = DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
                    if (hr != 0)
                    {
                        // Fallback: DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19
                        DwmSetWindowAttribute(handle, 19, ref darkMode, sizeof(int));
                    }
                }
            }
            catch
            {
                // Ignore DWM errors on unsupported platforms
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static bool IsWindowsInDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                {
                    return intVal == 0;
                }
            }
            catch
            {
                // Fallback to light mode on error
            }
            return false;
        }

        private static AppTheme LoadSavedTheme()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null && Enum.TryParse<AppTheme>(settings.Theme, true, out var theme))
                    {
                        return theme;
                    }
                }
            }
            catch
            {
                // Ignore load error
            }
            return AppTheme.System;
        }

        private static void SaveTheme(AppTheme theme)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var settings = new AppSettings { Theme = theme.ToString() };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Ignore save error
            }
        }

        private class AppSettings
        {
            public string Theme { get; set; } = "System";
        }
    }
}
