using Microsoft.Win32;
using System.Windows;

namespace Calendar.Services
{
    public static class ThemeService
    {
        public static string CurrentTheme { get; private set; } = "System";


        public static void SetTheme(string theme)
        {
            CurrentTheme = theme;

            string actualTheme = theme;

            if (theme == "System")
            {
                actualTheme =
                    IsWindowsUsingLightTheme()
                        ? "Light"
                        : "Dark";
            }


            string resourcePath =
                actualTheme == "Dark"
                    ? "/Themes/DarkTheme.xaml"
                    : "/Themes/LightTheme.xaml";


            ResourceDictionary newTheme =
                new ResourceDictionary
                {
                    Source = new Uri(
                        resourcePath,
                        UriKind.Relative)
                };


            // Remove ALL previously loaded theme dictionaries.
            var existingThemes =
                Application.Current.Resources
                    .MergedDictionaries
                    .Where(dictionary =>
                    {
                        string? source =
                            dictionary.Source?.OriginalString;

                        if (string.IsNullOrWhiteSpace(source))
                        {
                            return false;
                        }

                        return
                            source.EndsWith(
                                "LightTheme.xaml",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            source.EndsWith(
                                "DarkTheme.xaml",
                                StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();


            foreach (ResourceDictionary dictionary
                     in existingThemes)
            {
                Application.Current.Resources
                    .MergedDictionaries
                    .Remove(dictionary);
            }


            // IMPORTANT:
            // Add at the END so this theme has priority.
            Application.Current.Resources
                .MergedDictionaries
                .Add(newTheme);
        }


        private static bool IsWindowsUsingLightTheme()
        {
            try
            {
                object? value =
                    Registry.GetValue(
                        @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "AppsUseLightTheme",
                        1);

                if (value is int result)
                {
                    return result != 0;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}