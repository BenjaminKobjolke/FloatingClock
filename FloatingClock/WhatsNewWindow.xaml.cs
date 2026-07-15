using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using FloatingClock.Managers;

namespace FloatingClock
{
    /// <summary>
    /// Shows the bundled release notes, newest release first, with Older/Newer navigation
    /// </summary>
    public partial class WhatsNewWindow : Window
    {
        private class ReleaseNote
        {
            public string Label;
            public Version Version;
            public int Build;
            public string Date;
            public string Title;
            public List<string> Notes;
        }

        private const string ResourcePrefix = "FloatingClock.release_notes.";

        private readonly List<ReleaseNote> releases;
        private int currentIndex;

        public WhatsNewWindow(bool isDark)
        {
            InitializeComponent();

            Title = LocalizationManager.Lang("release_notes.title");
            OlderButton.Content = LocalizationManager.Lang("release_notes.older");
            NewerButton.Content = LocalizationManager.Lang("release_notes.newer");
            CloseButton.Content = LocalizationManager.Lang("release_notes.close");

            releases = LoadReleases();
            ApplyTheme(isDark);
            ShowRelease();
        }

        /// <summary>
        /// Loads all embedded release notes in the current UI language (fallback: English),
        /// sorted newest first by the release folder label (e.g. "2.0.0_1")
        /// </summary>
        private static List<ReleaseNote> LoadReleases()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Group resource names by release label: "2.0.0_1\en.json" -> label "2.0.0_1", file "en.json"
            var byLabel = new Dictionary<string, Dictionary<string, string>>();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(ResourcePrefix)) continue;

                var parts = name.Substring(ResourcePrefix.Length).Split('\\', '/');
                if (parts.Length != 2) continue;

                if (!byLabel.TryGetValue(parts[0], out var localeFiles))
                {
                    localeFiles = new Dictionary<string, string>();
                    byLabel[parts[0]] = localeFiles;
                }
                localeFiles[parts[1]] = name;
            }

            string preferredFile = LocalizationManager.CurrentLanguage + ".json";
            var result = new List<ReleaseNote>();
            foreach (var entry in byLabel)
            {
                string resourceName;
                if (!entry.Value.TryGetValue(preferredFile, out resourceName) &&
                    !entry.Value.TryGetValue("en.json", out resourceName))
                {
                    continue;
                }

                var release = ParseRelease(assembly, resourceName, entry.Key);
                if (release != null)
                {
                    result.Add(release);
                }
            }

            return result
                .OrderByDescending(r => r.Version)
                .ThenByDescending(r => r.Build)
                .ToList();
        }

        private static ReleaseNote ParseRelease(Assembly assembly, string resourceName, string label)
        {
            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reader.ReadToEnd());

                    var notes = new List<string>();
                    if (data.TryGetValue("notes", out object rawNotes) && rawNotes is IEnumerable enumerable)
                    {
                        foreach (var note in enumerable)
                        {
                            notes.Add(note.ToString());
                        }
                    }

                    // The folder label (e.g. "2.0.0_1") is the source of truth for ordering
                    int separator = label.LastIndexOf('_');
                    return new ReleaseNote
                    {
                        Label = label,
                        Version = Version.Parse(label.Substring(0, separator)),
                        Build = int.Parse(label.Substring(separator + 1)),
                        Date = data.TryGetValue("date", out object date) ? date.ToString() : "",
                        Title = data.TryGetValue("title", out object title) ? title.ToString() : "",
                        Notes = notes
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        private void ShowRelease()
        {
            if (releases.Count == 0)
            {
                VersionText.Text = "-";
                OlderButton.IsEnabled = false;
                NewerButton.IsEnabled = false;
                return;
            }

            var release = releases[currentIndex];
            VersionText.Text = $"{release.Label} — {release.Date}";
            TitleText.Text = release.Title;
            NotesList.ItemsSource = release.Notes;
            OlderButton.IsEnabled = currentIndex < releases.Count - 1;
            NewerButton.IsEnabled = currentIndex > 0;
        }

        private void ApplyTheme(bool isDark)
        {
            var textBrush = new SolidColorBrush(isDark ? Colors.White : Color.FromRgb(51, 51, 51));
            Background = new SolidColorBrush(isDark ? Color.FromRgb(45, 45, 45) : Color.FromRgb(240, 240, 240));
            VersionText.Foreground = textBrush;
            TitleText.Foreground = textBrush;
            NotesList.Foreground = textBrush;
        }

        private void OlderButton_Click(object sender, RoutedEventArgs e)
        {
            currentIndex++;
            ShowRelease();
        }

        private void NewerButton_Click(object sender, RoutedEventArgs e)
        {
            currentIndex--;
            ShowRelease();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
