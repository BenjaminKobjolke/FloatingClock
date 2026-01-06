using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FloatingClock.Managers
{
    /// <summary>
    /// Simple localization manager that reads from embedded JSON resources
    /// </summary>
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _translations;
        private static readonly object _lock = new object();
        private static string _currentLanguage = "en";

        /// <summary>
        /// Initializes the localization system
        /// </summary>
        private static void Initialize()
        {
            if (_translations != null) return;

            lock (_lock)
            {
                if (_translations != null) return;

                _translations = new Dictionary<string, string>();

                // Try to load system language first, fall back to English
                string systemLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                if (!TryLoadLanguage(systemLang))
                {
                    TryLoadLanguage("en");
                }
            }
        }

        /// <summary>
        /// Attempts to load a language file from embedded resources
        /// </summary>
        private static bool TryLoadLanguage(string langCode)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"FloatingClock.lang.{langCode}.json";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return false;

                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        ParseJson(json, "");
                        _currentLanguage = langCode;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Simple JSON parser that flattens nested objects into dot-notation keys
        /// </summary>
        private static void ParseJson(string json, string prefix)
        {
            // Simple JSON parsing for nested string values
            // Format: "key": "value" or "key": { nested }
            var pattern = @"""([^""]+)""\s*:\s*(""([^""]*)""|(\{[^{}]*\}))";
            var matches = Regex.Matches(json, pattern);

            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string fullKey = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

                if (match.Groups[3].Success)
                {
                    // String value
                    _translations[fullKey] = match.Groups[3].Value;
                }
                else if (match.Groups[4].Success)
                {
                    // Nested object - recurse
                    ParseJson(match.Groups[4].Value, fullKey);
                }
            }
        }

        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        /// <param name="key">The translation key (e.g., "app.title")</param>
        /// <returns>The localized string, or the key if not found</returns>
        public static string Lang(string key)
        {
            Initialize();

            if (_translations.TryGetValue(key, out string value))
            {
                return value;
            }

            // Return key as fallback
            return key;
        }

        /// <summary>
        /// Gets a localized string by key with placeholder replacements
        /// </summary>
        /// <param name="key">The translation key</param>
        /// <param name="replacements">Dictionary of placeholder replacements (e.g., ":name" -> "John")</param>
        /// <returns>The localized string with replacements</returns>
        public static string Lang(string key, Dictionary<string, string> replacements)
        {
            string result = Lang(key);

            if (replacements != null)
            {
                foreach (var kvp in replacements)
                {
                    result = result.Replace(kvp.Key, kvp.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Changes the current language
        /// </summary>
        /// <param name="langCode">Language code (e.g., "en", "de")</param>
        public static void SetLanguage(string langCode)
        {
            lock (_lock)
            {
                _translations = new Dictionary<string, string>();
                if (!TryLoadLanguage(langCode))
                {
                    TryLoadLanguage("en");
                }
            }
        }

        /// <summary>
        /// Gets the current language code
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;
    }
}
