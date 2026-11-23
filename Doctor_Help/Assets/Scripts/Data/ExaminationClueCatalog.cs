using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DoctorHelp
{
    public enum ClueMeasurement { None, Temperature, Heartbeat }

    [CreateAssetMenu(menuName = "Doctor Help/Examination Clue Catalog", fileName = "ExaminationClueCatalog")]
    public sealed class ExaminationClueCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string symptomId;
            public ClueMeasurement measurement;
            [Tooltip("Literal affirmative phrases that may be revealed in dialogue. Matching is case-insensitive and checks word boundaries.")]
            public string[] dialoguePhrases = Array.Empty<string>();
            [Tooltip("Exact wordings inside legacy <symptom> tags in the casebook. IDs, not disease/list order, determine highlighting.")]
            public string[] referencePhrases = Array.Empty<string>();
        }
        [SerializeField] private SymptomProfile symptoms;
        [SerializeField] private List<Entry> entries = new List<Entry>();
        public IReadOnlyList<Entry> Entries => entries;
        public SymptomProfile Symptoms => symptoms;
        private readonly Dictionary<string, Regex> phrases = new Dictionary<string, Regex>();
        private static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex Interjection = new Regex(@"—[^—\r\n]*—", RegexOptions.Compiled);
        private static readonly Regex Negated = new Regex(@"(?:\bno|\bnot|\bwithout|\bdon't have|\bdo not have|\bdon't feel|\bdo not feel|\bnever feel)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public Entry Find(string id) => entries.Find(e => e != null && e.symptomId == id);
        public static string Normalize(string text) => Spaces.Replace(text ?? "", " ").Trim();
        public IEnumerable<(Entry entry, int start, int length, string caption)> DialogueMatches(string text)
        {
            // Ignore inserted Brain Rot syllables for matching only; original text/indices remain intact.
            string searchable = Interjection.Replace(text ?? "", m => new string(' ', m.Length));
            foreach (var entry in entries)
            {
                if (entry == null || symptoms == null || symptoms.Find(entry.symptomId) == null) continue;
                foreach (string phrase in entry.dialoguePhrases)
                {
                    if (string.IsNullOrWhiteSpace(phrase)) continue;
                    if (!phrases.TryGetValue(phrase, out var regex))
                    {
                        string pattern = Regex.Escape(Normalize(phrase)).Replace("\\ ", "\\s+");
                        regex = new Regex(@"(?<![\p{L}\p{N}_])" + pattern + @"(?![\p{L}\p{N}_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                        phrases.Add(phrase, regex);
                    }
                    foreach (Match match in regex.Matches(searchable))
                    {
                        string before = searchable.Substring(Math.Max(0, match.Index - 48), Math.Min(48, match.Index));
                        if (Negated.IsMatch(before)) continue;
                        // Keep the source span for reveal timing/flight origin, but fly only the
                        // symptom's short name, even when its authored alias is a whole clause.
                        string caption = Normalize(symptoms.Find(entry.symptomId).DisplayName);
                        yield return (entry, match.Index, match.Length, caption);
                    }
                }
            }
        }
        public string ResolveReference(string text, IReadOnlyList<string> symptomIds)
        {
            string normalized = Normalize(text); string result = null;
            foreach (string id in symptomIds)
            {
                var entry = Find(id); bool matches = symptoms?.Find(id) != null && string.Equals(symptoms.Find(id).DisplayName, normalized, StringComparison.OrdinalIgnoreCase);
                if (entry != null) foreach (string alias in entry.referencePhrases) matches |= string.Equals(Normalize(alias), normalized, StringComparison.OrdinalIgnoreCase);
                if (!matches) continue;
                if (result != null && result != id) return null; // Ambiguous authoring is never guessed by list order.
                result = id;
            }
            return result;
        }
        private void OnValidate() => phrases.Clear();
    }
}
