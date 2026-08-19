// Scripts/Utils/CountryIsoUtil.cs
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BumiMobile
{
    public static class CountryIsoUtil
    {
        static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "UK", "GB" }, { "U.K.", "GB" }, { "Great Britain", "GB" }, { "Britain", "GB" },
            { "South Korea", "KR" }, { "Republic of Korea", "KR" }, { "Korea, Republic of", "KR" },
            { "North Korea", "KP" }, { "Korea, Democratic People's Republic of", "KP" },
            { "USA", "US" }, { "U.S.A.", "US" }, { "United States", "US" }, { "United States of America", "US" },
            { "Russia", "RU" }, { "Viet Nam", "VN" }, { "UAE", "AE" }, { "Czech Republic", "CZ" }
        };

        static readonly Dictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static string ToIso2OrEmpty(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim();
            if (s.Length == 2 && char.IsLetter(s[0]) && char.IsLetter(s[1])) return s.ToUpperInvariant();
            if (Aliases.TryGetValue(s, out var iso)) return iso;
            if (Cache.TryGetValue(s, out iso)) return iso;

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
                    catch { }
                }
            }
            catch { }

            return "";
        }
    }
}
