using System.Globalization;
using System.Windows;

namespace Calendar.Localization
{
    public static class LocalizationService
    {
        public static readonly IReadOnlyList<LanguageDefinition> Languages =
            new List<LanguageDefinition>

            ///////////////////////////////////////
            ///                                 ///
            ///         Language template       ///
            /// Code = "",                      ///
            /// DisplayName = "",               ///
            /// CultureName = "",               ///
            /// ResourcePath ="",               ///
            ///                                 ///
            ///////////////////////////////////////

            {
                new()
                {
                    Code = "en",
                    DisplayName = "English",
                    CultureName = "en-GB",
                    ResourcePath = "/Languages/Strings.en.xaml"
                },

                new()
                {
                    Code = "nl",
                    DisplayName = "Nederlands",
                    CultureName = "nl-NL",
                    ResourcePath = "/Languages/Strings.nl.xaml"
                },
                new() 
                {
                    Code = "fY",
                    DisplayName = "Frysk",
                    CultureName = "fy-NL",
                    ResourcePath = "/Languages/Strings.fy.xaml"
                },
                new() 
                {
                    Code = "gro",
                    DisplayName = "Gronings",
                    CultureName = "nl-NL",
                    ResourcePath = "/Languages/Strings.gro.xaml"
                },
                new() 
                {
                    Code = "bra",
                    DisplayName = "Brabants",
                    CultureName = "nl-NL",
                    ResourcePath = "/Languages/Strings.bra.xaml"
                },

                new() 
                {
                    Code = "de",
                    DisplayName = "Deutsch",
                    CultureName = "de-DE",
                    ResourcePath = "/Languages/Strings.de.xaml"
                },

                new() 
                {
                    Code = "es",
                    DisplayName = "Español",
                    CultureName = "es-ES",
                    ResourcePath = "/Languages/Strings.es.xaml"
                },
                new() 
                {
                    Code = "it",
                    DisplayName = "Italiano",
                    CultureName = "it-IT",
                    ResourcePath = "/Languages/Strings.it.xaml"
                },
                new() 
                {
                    Code = "pt",
                    DisplayName = "Português",
                    CultureName = "pt-PT",
                    ResourcePath = "/Languages/Strings.pt.xaml"
                },

                new() 
                {
                    Code = "zh-Hans",
                    DisplayName = "中文（简体）",
                    CultureName = "zh-CN",
                    ResourcePath = "/Languages/Strings.zh-Hans.xaml"
                },
                new() 
                {
                    Code = "zh-Hant",
                    DisplayName = "中文（繁體）",
                    CultureName = "zh-TW",
                    ResourcePath = "/Languages/Strings.zh-Hant.xaml"
                },

                new()
                {
                    Code = "ja",
                    DisplayName = "日本語",
                    CultureName = "ja-JP",
                    ResourcePath = "/Languages/Strings.ja.xaml"
                },

                new()
                {
                    Code = "la",
                    DisplayName = "Latin",
                    CultureName = "en-GB",
                    ResourcePath = "/Languages/Strings.la.xaml"
                },

                new()
                {
                    Code = "fr",
                    DisplayName = "Français",
                    CultureName = "fr-FR",
                    ResourcePath = "/Languages/Strings.fr.xaml"
                },

                new()
                {
                    Code = "hi",
                    DisplayName = "हिन्दी",
                    CultureName = "hi-IN",
                    ResourcePath = "/Languages/Strings.hi.xaml"
                },

                new()
                {
                    Code = "ar",
                    DisplayName = "العربية",
                    CultureName = "ar-SA",
                    ResourcePath = "/Languages/Strings.ar.xaml",
                    isRightToLeft = true
                }
            };

        public static string CurrentLanguagesCode { get; private set; } = "en";

        public static void SetLanguage(string languageCode)
        {
            LanguageDefinition? language =
                Languages.FirstOrDefault(
                    x => x.Code == languageCode);

            language ??=
                Languages.First(
                    x => x.Code == "en");

            ResourceDictionary dictionary =
                new ResourceDictionary
                {
                    Source = new Uri(
                        language.ResourcePath,
                        UriKind.RelativeOrAbsolute)
                };

            ResourceDictionary? oldLanguageDictionary =
                Application.Current.Resources
                .MergedDictionaries
                .FirstOrDefault(
                    dictionary =>
                        dictionary.Source?.OriginalString
                            .Contains(
                                "/Languages/Strings."
                            )
                      == true
                    );

            if (oldLanguageDictionary != null)
            {
                Application.Current.Resources
                    .MergedDictionaries
                    .Remove( oldLanguageDictionary );
            }

            Application.Current.Resources
                .MergedDictionaries
                .Add(dictionary);

            CultureInfo culture =
                new CultureInfo(language.CultureName);

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            FlowDirection direction =
                language.isRightToLeft
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight;

            foreach (Window window in
                Application.Current.Windows)
            {
                window.FlowDirection = direction;
            }
        }

        public static string Get(string key)
        {
            object? value =
                Application.Current.TryFindResource(key);

            return value?.ToString() ?? key;
        }
    }
}
