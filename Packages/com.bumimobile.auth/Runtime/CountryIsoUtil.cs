// Scripts/Utils/CountryIsoUtil.cs
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BumiMobile
{
    public static class CountryIsoUtil
    {
        // Quick alias fixups & weird cases you’re likely to see
        static readonly Dictionary<string, string> Aliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "UK", "GB" }, { "U.K.", "GB" }, { "Great Britain", "GB" }, { "Britain", "GB" },
                { "South Korea", "KR" }, { "Republic of Korea", "KR" }, { "Korea, Republic of", "KR" },
                { "North Korea", "KP" }, { "Korea, Democratic People's Republic of", "KP" },
                { "USA", "US" }, { "U.S.A.", "US" }, { "United States", "US" }, { "United States of America", "US" },
                { "Russia", "RU" }, { "Viet Nam", "VN" }, { "UAE", "AE" }, { "Czech Republic", "CZ" }
            };

        // Cache for name → ISO2 lookups (RegionInfo can be a bit heavy)
        static readonly Dictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static string ToIso2OrEmpty(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            // Already ISO-2?
            var s = input.Trim();
            if (s.Length == 2 && char.IsLetter(s[0]) && char.IsLetter(s[1]))
                return s.ToUpperInvariant();

            // Aliases?
            if (Aliases.TryGetValue(s, out var iso))
                return iso;

            // Cached?
            if (Cache.TryGetValue(s, out iso))
                return iso;

            // Try match by English/Native names via RegionInfo
            try
            {
                foreach (var c in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                {
                    try
                    {
                        var r = new RegionInfo(c.Name);
                        if (string.Equals(r.EnglishName, s, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.NativeName, s, StringComparison.OrdinalIgnoreCase))
                        {
                            iso = r.TwoLetterISORegionName.ToUpperInvariant();
                            Cache[s] = iso;
                            return iso;
                        }
                    }
                    catch { /* skip invalid */ }
                }
            }
            catch { }

            return ""; // unknown
        }
    }
}
