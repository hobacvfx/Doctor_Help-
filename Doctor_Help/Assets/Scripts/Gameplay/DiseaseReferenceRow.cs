using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class DiseaseReferenceRow : MonoBehaviour
    {
        [SerializeField] private Text numberText, nameText, descriptionText;
        [SerializeField] private RectTransform illustrationsRoot;
        [SerializeField] private Image illustrationPrefab;
        [SerializeField] private string emptyDescription = "No symptoms recorded.";
        [SerializeField] private Color symptomColor = new Color(.37f, .15f, .51f);
        private readonly List<Image> images = new List<Image>();

        public void Bind(SicknessDefinition disease, int number, SymptomProfile symptoms, SymptomReferenceArt art)
        {
            if (disease == null || numberText == null || nameText == null || descriptionText == null ||
                illustrationsRoot == null || illustrationPrefab == null)
                throw new InvalidOperationException($"Casebook row '{name}' references are incomplete: disease={disease != null}, number={numberText != null}, name={nameText != null}, description={descriptionText != null}, artRoot={illustrationsRoot != null}, imagePrefab={illustrationPrefab != null}.");
            numberText.text = number.ToString("00") + ".";
            if (images.Count == 0) images.AddRange(illustrationsRoot.GetComponentsInChildren<Image>(true));
            nameText.text = disease.DisplayName;
            string open = "<color=#" + ColorUtility.ToHtmlStringRGB(symptomColor) + "><b>";
            const string close = "</b></color>";
            var names = new List<string>();
            var sprites = new List<Sprite>();
            var seen = new HashSet<string>();
            foreach (string id in disease.SymptomIds)
            {
                var symptom = symptoms.Find(id);
                if (symptom == null || !seen.Add(id))
                    throw new InvalidOperationException($"Casebook: '{disease.DisplayName}' contains a missing/duplicate symptom ID '{id}'.");
                names.Add(open + symptom.DisplayName.Replace("<", "‹").Replace(">", "›") + close);
                var sprite = art.Find(id);
                if (sprite != null) sprites.Add(sprite);
            }
            descriptionText.supportRichText = true;
            descriptionText.text = !string.IsNullOrWhiteSpace(disease.ReferenceDescription)
                ? disease.ReferenceDescription.Replace("<symptom>", open).Replace("</symptom>", close)
                : names.Count == 0 ? emptyDescription : string.Join("; ", names) + ".";
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
        }

        private void OnRectTransformDimensionsChange() => LayoutIllustrations();
        private void LayoutIllustrations()
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
