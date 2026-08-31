using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.Localization
{
    public class LanguageDefinition
    {
        public string Code { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public string CultureName { get; set; } = "";

        public string ResourcePath { get; set; } = "";

        public bool isRightToLeft { get; set; }
    }
}
