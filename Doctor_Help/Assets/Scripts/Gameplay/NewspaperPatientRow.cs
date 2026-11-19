using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class NewspaperPatientRow : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Text illness, required, action;
        [SerializeField] private Text goldAmount;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private string jailedText = "Jailed";
        [SerializeField] private NewspaperStarGraphic[] feedbackStars;
        [SerializeField] private ParticleSystem feedbackStarEffect;
        [SerializeField] private Color correctColor = new Color(.19f, .34f, .12f);
        [SerializeField] private Color incorrectColor = new Color(.65f, .08f, .04f);

        public Text GoldAmount => goldAmount;
        public int GoldDelta { get; private set; }
        private string feedbackText;
        private ParticleSystem.MinMaxCurve starFrames;
        private int starFrameCount, starCycles;
        private RectTransform starViewport;
        private Vector2 starLoopRange;
        private float starDuration, starElapsed;

        public void Bind(PatientTreatmentResult patient, NewspaperPortraitCatalog portraits, RectTransform viewport, bool animateGold)
        {
            portrait.sprite = portraits.Find(patient.NpcId);
            portrait.enabled = portrait.sprite != null;
            // Portraits are optional presentation assets. A missing entry must not block
            // the results screen or the player's ability to continue to the next day.
            if (portrait.sprite == null)
                Debug.LogWarning("Missing newspaper portrait for " + patient.NpcName + ". Refresh the Newspaper Portraits catalog in the Editor.", this);
            illness.text = "<b>Illness:</b> " + Escape(patient.Illness.Length > 0 ? patient.Illness : "None");
            required.text = "<b>Required:</b> " + Format(patient.Required);
            action.text = "<b>Your action:</b> " + Format(patient.Action);
            feedbackText ??= feedbackLabel.text;
            feedbackLabel.text = patient.IsJailed ? jailedText : feedbackText;
            feedbackLabel.transform.parent.gameObject.SetActive(patient.IsJailed || patient.FeedbackStars.HasValue);
            ConfigureStars(patient.FeedbackStars, viewport);
            GoldDelta = patient.GoldDelta;
            goldAmount.color = GoldDelta > 0 ? correctColor : incorrectColor;
            goldAmount.text = animateGold ? "" : FormatGold(GoldDelta);
        }

        private void ConfigureStars(float? rating, RectTransform viewport)
        {
            var sheet = feedbackStarEffect.textureSheetAnimation;
            var effect = feedbackStarEffect.main;
            var atlas = feedbackStarEffect.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetTexture("_BaseMap");
            starFrameCount = Mathf.Max(1, sheet.numTilesX);
            starCycles = sheet.cycleCount;
            starFrames = sheet.frameOverTime;
            starViewport = viewport;
            starLoopRange = effect.ringBufferMode == ParticleSystemRingBufferMode.LoopUntilReplaced
                ? effect.ringBufferLoopRange : Vector2.zero;
            starDuration = rating.GetValueOrDefault() > 0 ? effect.startLifetime.constantMax : 0;
            starElapsed = 0;
            for (int i = 0; i < feedbackStars.Length; i++)
                feedbackStars[i].Configure(atlas, rating.GetValueOrDefault() - i);
            ShowStarFrame(0);
        }

        private void LateUpdate()
        {
            if (starDuration <= 0) return;
            var firstStar = feedbackStars[0].rectTransform;
            Vector3 center = starViewport.InverseTransformPoint(firstStar.TransformPoint(firstStar.rect.center));
            if (!starViewport.rect.Contains(center)) return;
            AdvanceStarAnimation(Time.unscaledDeltaTime);
        }

        private void AdvanceStarAnimation(float deltaTime)
        {
            float progress = starElapsed / starDuration;
            if (progress >= 1f)
            {
                float loopLength = starLoopRange.y - starLoopRange.x;
                if (loopLength <= 0)
                {
                    starDuration = 0;
                    ShowStarFrame(0);
                    return;
                }
                progress = starLoopRange.x + Mathf.Repeat(progress - 1f, loopLength);
                starElapsed = progress * starDuration;
            }

            int frame = Mathf.FloorToInt(Mathf.Repeat(starFrames.Evaluate(progress) * starCycles, 1) * starFrameCount);
            ShowStarFrame(frame);
            starElapsed += deltaTime;
        }

        private void ShowStarFrame(int frame)
        {
            foreach (var star in feedbackStars)
                if (star.gameObject.activeSelf) star.ShowFrame(frame, starFrameCount);
        }

        public void ShowGold(int amount) => goldAmount.text = FormatGold(amount);
        private static string FormatGold(int amount) => amount.ToString("+0;-0;0");

        private string Format(IReadOnlyList<TreatmentTextPart> parts)
        {
            var text = new StringBuilder();
            foreach (var part in parts)
            {
                if (text.Length > 0) text.Append(" + ");
                string value = Escape(part.Text);
                if (part.Tone == TreatmentTextTone.Normal) text.Append(value);
                else text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(
                    part.Tone == TreatmentTextTone.Correct ? correctColor : incorrectColor))
                    .Append('>').Append(value).Append("</color>");
            }
            return text.ToString();
        }
        private static string Escape(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");
    }
}
