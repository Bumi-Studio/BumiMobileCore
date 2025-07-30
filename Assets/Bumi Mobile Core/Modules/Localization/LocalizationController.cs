using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BumiMobile
{
	/// <summary>
	/// Localization manager.
	/// </summary>
    public static class LocalizationController
    {
		/// <summary>
		/// Fired when localization changed.
		/// </summary>
        public static event Action OnLocalizationChanged = () => { }; 

        public static Dictionary<LanguageType, Dictionary<string, string>> Dictionary = new();
        private static LanguageType _language = LanguageType.English;
        private static LocalizationSettings _settings;

		/// <summary>
		/// Get or set language.
		/// </summary>
        public static LanguageType Language
        {
            get => _language;
            set { _language = value; OnLocalizationChanged(); }
        }

		/// <summary>
		/// Set default language.
		/// </summary>
        public static void AutoLanguage()
        {
            Language = LanguageType.English;
        }

        public static void Init(LocalizationSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Read localization spreadsheets.
        /// </summary>
        public static void Read()
        {
            if (Dictionary.Count > 0) return;

            var keys = new List<string>();

            foreach (var sheet in _settings.Sheets)
            {
                var textAsset = sheet.TextAsset;
                var lines = GetLines(textAsset.text);
				var languages = lines[0].Split(',').Select(i => i.Trim()).ToList();

                if (languages.Count != languages.Distinct().Count())
                {
                    Debug.LogError($"Duplicated languages found in `{sheet.Name}`. This sheet is not loaded.");
                    continue;
                }

                for (var i = 1; i < languages.Count; i++)
                {
                    LanguageType language = languages[i].ToEnum(false, LanguageType.English);
                    if (!Dictionary.ContainsKey(language))
                    {
                        Dictionary.Add(language, new Dictionary<string, string>());
                    }
                }

                for (var i = 1; i < lines.Count; i++)
                {
                    var columns = GetColumns(lines[i]);
                    var key = columns[0];

                    if (key == "") continue;

                    if (keys.Contains(key))
                    {
                        Debug.LogError($"Duplicated key `{key}` found in `{sheet.Name}`. This key is not loaded.");
                        continue;
                    }

                    keys.Add(key);

                    for (var j = 1; j < languages.Count; j++)
                    {
                        LanguageType language = languages[j].ToEnum(false, LanguageType.English);
                        if (Dictionary[language].ContainsKey(key))
                        {
                            Debug.LogError($"Duplicated key `{key}` in `{sheet.Name}`.");
                        }
                        else
                        {
                            Dictionary[language].Add(key, columns[j]);
                        }
                    }
                }
            }

            AutoLanguage();
        }

        /// <summary>
        /// Check if a key exists in localization.
        /// </summary>
        public static bool HasKey(string localizationKey)
        {
            return Dictionary.ContainsKey(Language) && Dictionary[Language].ContainsKey(localizationKey);
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static string Localize(string localizationKey)
        {
            if (Dictionary.Count == 0)
            {
                Read();
            }

            if (!Dictionary.ContainsKey(Language)) throw new KeyNotFoundException("Language not found: " + Language);

            var missed = !Dictionary[Language].ContainsKey(localizationKey) || Dictionary[Language][localizationKey] == "";

            if (missed)
            {
                Debug.LogWarning($"Translation not found: {localizationKey} ({Language}).");

                return Dictionary[LanguageType.English].ContainsKey(localizationKey) ? Dictionary[LanguageType.English][localizationKey] : localizationKey;
            }

            return Dictionary[Language][localizationKey];
        }

	    /// <summary>
	    /// Get localized value by localization key.
	    /// </summary>
		public static string Localize(string localizationKey, params object[] args)
        {
            var pattern = Localize(localizationKey);

            return string.Format(pattern, args);
        }

        public static List<string> GetLines(string text)
        {
            text = text.Replace("\r\n", "\n").Replace("\"\"", "[_quote_]");
            
            var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");

            foreach (Match match in matches)
            {
                text = text.Replace(match.Value, match.Value.Replace("\"", null).Replace(",", "[_comma_]").Replace("\n", "[_newline_]"));
            }

            // Making uGUI line breaks to work in asian texts.
            text = text.Replace("。", "。 ").Replace("、", "、 ").Replace("：", "： ").Replace("！", "！ ").Replace("（", " （").Replace("）", "） ").Trim();

            return text.Split('\n').Where(i => i != "").ToList();
        }

        public static List<string> GetColumns(string line)
        {
            return line.Split(',').Select(j => j.Trim()).Select(j => j.Replace("[_quote_]", "\"").Replace("[_comma_]", ",").Replace("[_newline_]", "\n")).ToList();
        }
    }
}