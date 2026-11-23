using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class DiseaseReferenceRow : MonoBehaviour
    {
        [SerializeField] private Text numberText, nameText, descriptionText;
        [SerializeField] private RectTransform illustrationsRoot;
        [SerializeField] private Image illustrationPrefab;
        [SerializeField] private RectTransform treatmentsRoot;
        [SerializeField] private Image treatmentPrefab;
        [SerializeField] private Sprite telephoneSprite;
        [SerializeField] private string emptyDescription = "No symptoms recorded.";
        [SerializeField] private Color symptomColor = new Color(.37f, .15f, .51f);
        private readonly List<Image> images = new List<Image>();
        private readonly List<Image> treatments = new List<Image>();
        private SicknessDefinition boundDisease;
        private SymptomProfile boundSymptoms;
        private DoctorBookClues boundClues;
        private ClueTextPulse cluePulse;
        private static readonly Regex SymptomTag = new Regex(@"<symptom(?:\s+id=['""]?([^'"">]+)['""]?)?>(.*?)</symptom>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public void Bind(SicknessDefinition disease, int number, SymptomProfile symptoms, SymptomReferenceArt art, PillProfile pills, DoctorBookClues clues = null)
        {
            if (disease == null || numberText == null || nameText == null || descriptionText == null ||
                illustrationsRoot == null || illustrationPrefab == null)
                throw new InvalidOperationException($"Casebook row '{name}' references are incomplete: disease={disease != null}, number={numberText != null}, name={nameText != null}, description={descriptionText != null}, artRoot={illustrationsRoot != null}, imagePrefab={illustrationPrefab != null}.");
            numberText.text = number.ToString("00") + ".";
            if (images.Count == 0) images.AddRange(illustrationsRoot.GetComponentsInChildren<Image>(true));
            nameText.text = disease.DisplayName;
            boundDisease = disease; boundSymptoms = symptoms; boundClues = clues;
            var sprites = new List<Sprite>();
            var seen = new HashSet<string>();
            foreach (string id in disease.SymptomIds)
            {
                var symptom = symptoms.Find(id);
                if (symptom == null || !seen.Add(id))
                    throw new InvalidOperationException($"Casebook: '{disease.DisplayName}' contains a missing/duplicate symptom ID '{id}'.");
                var sprite = art.Find(id);
                if (sprite != null) sprites.Add(sprite);
            }
            RefreshDiscovery();
            for (int i = 0; i < Mathf.Max(sprites.Count, images.Count); i++)
            {
                if (i >= images.Count) images.Add(Instantiate(illustrationPrefab, illustrationsRoot, false));
                bool visible = i < sprites.Count;
                images[i].gameObject.SetActive(visible);
                images[i].sprite = visible ? sprites[i] : null;
                images[i].preserveAspect = true;
                images[i].raycastTarget = false;
            }
            LayoutIllustrations();
            BindTreatments(disease, pills);
        }

        public void RefreshDiscovery()
        {
            if (boundDisease == null || descriptionText == null) return;
            if (cluePulse == null) descriptionText.TryGetComponent(out cluePulse);
            bool any = false;
            string Styled(string id, string text)
            {
                bool discovered = id != null && boundClues != null && boundClues.IsDiscovered(id);
                any |= discovered;
                Color color = discovered ? (cluePulse != null ? cluePulse.DiscoveredColor : new Color32(220,38,38,255)) : symptomColor;
                return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + "><b>" + text.Replace("<","‹").Replace(">","›") + "</b></color>";
            }
            descriptionText.supportRichText = true;
            if (!string.IsNullOrWhiteSpace(boundDisease.ReferenceDescription))
                descriptionText.text = SymptomTag.Replace(boundDisease.ReferenceDescription, match =>
                {
                    string id = match.Groups[1].Success ? match.Groups[1].Value.Trim() : boundClues?.Catalog?.ResolveReference(match.Groups[2].Value, boundDisease.SymptomIds);
                    bool belongs = false; foreach (string symptom in boundDisease.SymptomIds) belongs |= symptom == id;
                    return Styled(belongs ? id : null, match.Groups[2].Value);
                });
            else
            {
                var names = new List<string>();
                foreach (string id in boundDisease.SymptomIds) { var symptom = boundSymptoms.Find(id); if (symptom != null) names.Add(Styled(id, symptom.DisplayName)); }
                descriptionText.text = names.Count == 0 ? emptyDescription : string.Join("; ", names) + ".";
            }
            if (cluePulse != null) cluePulse.enabled = any;
        }

        private void BindTreatments(SicknessDefinition disease, PillProfile pills)
        {
            if (treatmentsRoot == null || treatmentPrefab == null || telephoneSprite == null || pills == null)
                throw new InvalidOperationException("Casebook treatment column references are incomplete.");
            if (treatments.Count == 0) treatments.AddRange(treatmentsRoot.GetComponentsInChildren<Image>(true));
            var sprites = new List<Sprite>();
            if (disease.Treatment == SicknessTreatment.ReportToPolice) sprites.Add(telephoneSprite);
            else if (disease.Treatment == SicknessTreatment.RequiredPills)
            {
                var used = new HashSet<string>();
                foreach (string id in disease.PillIds)
                {
                    var pill = pills.Find(id);
                    if (pill == null || !used.Add(id))
                        throw new InvalidOperationException($"Casebook: '{disease.DisplayName}' has a missing/duplicate medicine ID '{id}'.");
                    if (pill.Sprite == null)
                        throw new InvalidOperationException($"Casebook: medicine '{pill.DisplayName}' needs an icon.");
                    sprites.Add(pill.Sprite);
                }
            }
            for (int i = 0; i < Mathf.Max(sprites.Count, treatments.Count); i++)
            {
                if (i >= treatments.Count) treatments.Add(Instantiate(treatmentPrefab, treatmentsRoot, false));
                bool visible = i < sprites.Count;
                treatments[i].gameObject.SetActive(visible);
                treatments[i].sprite = visible ? sprites[i] : null;
                treatments[i].preserveAspect = true;
                treatments[i].raycastTarget = false;
            }
            LayoutImages(treatmentsRoot, treatments);
        }

        private void OnRectTransformDimensionsChange()
        {
            LayoutIllustrations();
            LayoutImages(treatmentsRoot, treatments);
        }
        private void LayoutIllustrations()
            => LayoutImages(illustrationsRoot, images);
        private static void LayoutImages(RectTransform illustrationsRoot, List<Image> images)
        {
            if (illustrationsRoot == null || images == null) return;
            int count = 0;
            foreach (var image in images) if (image != null && image.gameObject.activeSelf) count++;
            if (count == 0) return;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count)), rows = Mathf.CeilToInt((float)count / columns);
            float width = illustrationsRoot.rect.width / columns, height = illustrationsRoot.rect.height / rows;
            int index = 0;
            foreach (var image in images)
            {
                if (image == null || !image.gameObject.activeSelf) continue;
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(width, height);
                int row = index / columns, col = index % columns;
                int rowCount = Mathf.Min(columns, count - row * columns);
                rect.anchoredPosition = new Vector2((col - (rowCount - 1) * .5f) * width, ((rows - 1) * .5f - row) * height);
                index++;
            }
        }
    }
}
