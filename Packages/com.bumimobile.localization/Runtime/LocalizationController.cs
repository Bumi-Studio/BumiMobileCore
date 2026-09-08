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
        private const string PREFS_KEY = "wm_lang";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapLanguage()
        {
            LoadLanguageFromPrefsOrAuto();
        }

        /// <summary>
        /// Get or set language.
        /// </summary>
        public static LanguageType Language
        {
            get => _language;
            set
            {
                if (_language == value)
                    return;

                _language = value;

                PlayerPrefs.SetInt(PREFS_KEY, (int)_language);
                PlayerPrefs.Save();

                Debug.Log($"Localization language: {_language}");

                OnLocalizationChanged();
            }
        }

        /// <summary>
        /// Set language based on system language.
        /// </summary>
        public static void AutoLanguage()
        {
            Language = MapSystemLanguage(Application.systemLanguage);
        }

        public static void Init(LocalizationSettings settings)
        {
            _settings = settings;

            if (!PlayerPrefs.HasKey(PREFS_KEY))
            {
                LoadLanguageFromPrefsOrAuto();
            }
        }

        private static void LoadLanguageFromPrefsOrAuto()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                var raw = PlayerPrefs.GetInt(PREFS_KEY, (int)LanguageType.English);

                _language = Enum.IsDefined(typeof(LanguageType), raw) ? (LanguageType)raw : LanguageType.English;

                Debug.Log($"Localization language: {_language}");
            }
            else
            {
                _language = MapSystemLanguage(Application.systemLanguage);
            }
        }

        private static LanguageType MapSystemLanguage(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
            {
                SystemLanguage.English => LanguageType.English,
                SystemLanguage.Indonesian => LanguageType.Indonesian,
                SystemLanguage.Arabic => LanguageType.Arab,
                SystemLanguage.ChineseSimplified => LanguageType.MandarinSimplified,
                SystemLanguage.Chinese or SystemLanguage.ChineseTraditional => LanguageType.MandarinTraditional,
                SystemLanguage.Japanese => LanguageType.Japanese,
                SystemLanguage.Korean => LanguageType.Korean,
                SystemLanguage.Italian => LanguageType.Italian,
                SystemLanguage.Spanish => LanguageType.Spanish,
                SystemLanguage.French => LanguageType.French,
                SystemLanguage.German => LanguageType.German,
                SystemLanguage.Portuguese => LanguageType.Portuguese,
                SystemLanguage.Russian => LanguageType.Russian,
                SystemLanguage.Thai => LanguageType.Thai,
                SystemLanguage.Vietnamese => LanguageType.Vietnamese,
                SystemLanguage.Dutch => LanguageType.Dutch,
                SystemLanguage.Hindi => LanguageType.Hindi,
                SystemLanguage.Turkish => LanguageType.Turkish,
                _ => LanguageType.English
            };
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
                            // Rows may omit trailing cells. Treat those cells as blank so
                            // lookup can fall back to English instead of throwing.
                            Dictionary[language].Add(key, j < columns.Count ? columns[j] : "");
                        }
                    }
                }
            }

            // Only auto-set language if no preference was previously saved
            // The language was already set in BootstrapLanguage() or Init()
            if (!PlayerPrefs.HasKey(PREFS_KEY))
            {
                AutoLanguage();
            }
        }

        /// <summary>
        /// Check if a key exists in localization.
        /// </summary>
        public static bool HasKey(string localizationKey)
        {
            return Dictionary.ContainsKey(Language) && Dictionary[Language].ContainsKey(localizationKey);
        }
        public static string Localize(string localizationKey)
        {
            if (Dictionary.Count == 0)
                Read();

            string raw = LocalizeRaw(localizationKey);
            return FixIfArabic(raw);
        }

        public static string Localize(string localizationKey, params object[] args)
        {
            if (Dictionary.Count == 0)
                Read();

            string pattern = LocalizeRaw(localizationKey); // unshaped
            string formatted = string.Format(pattern, args);
            return FixIfArabic(formatted);
        }

        private static string LocalizeRaw(string localizationKey)
        {
            if (Dictionary.TryGetValue(Language, out var languageDictionary) &&
                languageDictionary != null &&
                languageDictionary.TryGetValue(localizationKey, out var translation) &&
                !string.IsNullOrWhiteSpace(translation))
            {
                return translation;
            }

            Debug.LogWarning($"Translation not found: {localizationKey} ({Language}).");

            if (Dictionary.TryGetValue(LanguageType.English, out var englishDictionary) &&
                englishDictionary != null &&
                englishDictionary.TryGetValue(localizationKey, out var englishTranslation) &&
                !string.IsNullOrWhiteSpace(englishTranslation))
            {
                return englishTranslation;
            }

            return localizationKey;
        }

        public static string FixIfArabic(string s)
        {
            if (Language == LanguageType.Arab && !string.IsNullOrEmpty(s))
                return ArabicFixer.Fix(s, false, false);
            return s;
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
