using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialOverlay : MaskableGraphic
    {
        [SerializeField] private RectTransform hand, caption;
        [SerializeField] private Text instruction;
        [SerializeField] private Button reviewNext;
        [SerializeField] private Color reviewMaskColor = new Color(0, 0, 0, .76f);
        [SerializeField, Min(.1f)] private float tapPeriod = 1.1f;
        [SerializeField, Min(0)] private float tapTravel = 15;
        [SerializeField] private AnimationCurve tapPose = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField, Min(1)] private float holdRingRadius = 38;
        [SerializeField, Min(1)] private float holdRingThickness = 9;
        [SerializeField, Min(0)] private float holdRingSideOffset = 140;
        [SerializeField] private Color holdRingTrack = new Color(.12f, .09f, .07f, .94f);
        [SerializeField] private Color holdRingFill = new Color(1f, .78f, .36f);

        private Rect opening;
        private Rect? readingArea;
        private bool hasOpening;
        private Vector2? holdRingCenter;
        private float holdProgress;
        private bool reviewing;
        private bool dimmed;
        internal bool ReviewConfirmed { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (reviewNext != null) reviewNext.onClick.AddListener(ConfirmReview);
        }

        protected override void OnDisable()
        {
            if (reviewNext != null) reviewNext.onClick.RemoveListener(ConfirmReview);
            SetReview(false);
            dimmed = false;
            base.OnDisable();
        }

        private void ConfirmReview() => ReviewConfirmed = reviewing;

        private void SetReview(bool enabled)
        {
            if (reviewing == enabled) return;
            reviewing = enabled;
            ReviewConfirmed = false;
            if (reviewNext != null) reviewNext.gameObject.SetActive(enabled);
            SetVerticesDirty();
        }

        public void Present(Rect screenBounds, TutorialStep step, Rect? readingArea = null,
            Rect? captionAvoidanceArea = null, bool showHand = true, float handScale = 1f)
        {
            this.readingArea = readingArea;
            gameObject.SetActive(true);
            SetReview(step.Action == TutorialAction.ReviewSymptoms || step.Action == TutorialAction.ExplainStain);
            SetDimmed(step.Action == TutorialAction.SelectClean);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenBounds.min, null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenBounds.max, null, out var max);
            var bounds = rectTransform.rect;
            float padding = step.Padding;
            var hole = Rect.MinMaxRect(Mathf.Clamp(min.x - padding, bounds.xMin, bounds.xMax),
                Mathf.Clamp(min.y - padding, bounds.yMin, bounds.yMax), Mathf.Clamp(max.x + padding, bounds.xMin, bounds.xMax),
                Mathf.Clamp(max.y + padding, bounds.yMin, bounds.yMax));
            if (!hasOpening || opening != hole)
            {
                hasOpening = true;
                opening = hole;
                SetVerticesDirty();
            }

            hand.gameObject.SetActive(showHand);
            caption.gameObject.SetActive(!string.IsNullOrEmpty(step.Instruction));
            if (instruction.text != step.Instruction) instruction.text = step.Instruction;
            float phase = Mathf.Repeat(Time.unscaledTime / tapPeriod, 1);
            float press = Mathf.Clamp01(tapPose.Evaluate(phase));
            Vector2 contact = screenBounds.center;
            if (step.Action == TutorialAction.UseTool)
                contact -= Vector2.Scale(step.HoldAreaOffset, new Vector2(Screen.width, Screen.height));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, contact, null, out var handAnchor);
            hand.anchoredPosition = handAnchor + step.HandOffset + Vector2.down * (tapTravel * handScale * (1 - press));
            hand.localScale = Vector3.one * (Mathf.Lerp(1, .9f, press) * handScale);

            float captionAreaTop = hole.yMax;
            float captionAreaBottom = hole.yMin;
            if (captionAvoidanceArea.HasValue)
            {
                Rect area = captionAvoidanceArea.Value;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, area.min, null, out var areaMin);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, area.max, null, out var areaMax);
                captionAreaTop = Mathf.Max(captionAreaTop, areaMax.y);
                captionAreaBottom = Mathf.Min(captionAreaBottom, areaMin.y);
            }
            float halfHeight = caption.rect.height * .5f;
            var nextRect = reviewing && reviewNext != null ? (RectTransform)reviewNext.transform : null;
            float nextHeight = nextRect != null ? nextRect.rect.height - nextRect.anchoredPosition.y : 0;
            float above = captionAreaTop + halfHeight + nextHeight + 22;
            float below = captionAreaBottom - halfHeight - 22;
            float minY = bounds.yMin + halfHeight + nextHeight + 18;
            bool belowFits = below >= minY;
            float y = (step.Action == TutorialAction.UseTool && belowFits) || above > bounds.yMax - halfHeight - 18 ? below : above;
            caption.anchoredPosition = new Vector2(0, Mathf.Clamp(y, minY, bounds.yMax - halfHeight - 18));
        }

        public void Block()
        {
            gameObject.SetActive(true);
            SetReview(false);
            SetDimmed(false);
            hand.gameObject.SetActive(false);
            caption.gameObject.SetActive(false);
            readingArea = null;
            if (!hasOpening) return;
            hasOpening = false;
            SetVerticesDirty();
        }

        private void SetDimmed(bool enabled)
        {
            if (dimmed == enabled) return;
            dimmed = enabled;
            SetVerticesDirty();
        }

        internal void ShowHoldProgress(Vector2 pointer, Vector2 magnifierOffset, float progress)
        {
            float scale = Screen.height / 1920f;
            float margin = (holdRingRadius + 8) * scale;
            float sideways = holdRingSideOffset * scale;
            var safe = ScreenSafeArea.Current;
            var point = pointer + magnifierOffset;
            point.x += point.x + sideways + margin <= safe.xMax ? sideways : -sideways;
            if (point.y + margin > safe.yMax) point.y = pointer.y - magnifierOffset.y;
            point.x = Mathf.Clamp(point.x, safe.xMin + margin, safe.xMax - margin);
            point.y = Mathf.Clamp(point.y, safe.yMin + margin, safe.yMax - margin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, point, null, out var center);
            hand.gameObject.SetActive(false);
            if (holdRingCenter == center && holdProgress == progress) return;
            holdRingCenter = center;
            holdProgress = progress;
            SetVerticesDirty();
        }

        internal void HideHoldProgress()
        {
            if (!holdRingCenter.HasValue) return;
            holdRingCenter = null;
            SetVerticesDirty();
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera)) return false;
            if (reviewing) return true;
            if (dimmed) return !readingArea.HasValue || !readingArea.Value.Contains(screenPoint);
            if (readingArea.HasValue && readingArea.Value.Contains(screenPoint)) return false;
            return !hasOpening ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local) ||
                !opening.Contains(local);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var bounds = rectTransform.rect;
            if (!hasOpening)
            {
                AddQuad(mesh, bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax);
            }
            else
            {
                AddQuad(mesh, bounds.xMin, bounds.yMin, bounds.xMax, opening.yMin);
                AddQuad(mesh, bounds.xMin, opening.yMax, bounds.xMax, bounds.yMax);
                AddQuad(mesh, bounds.xMin, opening.yMin, opening.xMin, opening.yMax);
                AddQuad(mesh, opening.xMax, opening.yMin, bounds.xMax, opening.yMax);
            }
            if (holdRingCenter.HasValue)
            {
                float scale = bounds.height / 1920f;
                float radius = holdRingRadius * scale;
                float thickness = Mathf.Min(holdRingThickness * scale, radius);
                AddRing(mesh, holdRingCenter.Value, radius, thickness, 1, holdRingTrack);
                if (holdProgress > 0)
                    AddRing(mesh, holdRingCenter.Value, radius - thickness * .12f, thickness * .76f,
                        holdProgress, holdRingFill);
            }
        }

        internal static void AddRing(VertexHelper mesh, Vector2 center, float radius, float thickness,
            float fill, Color tint)
        {
            int segments = Mathf.CeilToInt(64 * fill);
            int first = mesh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (2 * Mathf.PI * fill * i) / segments;
                var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                mesh.AddVert(center + direction * radius, tint, Vector2.zero);
                mesh.AddVert(center + direction * (radius - thickness), tint, Vector2.zero);
                if (i == 0) continue;
                int edge = first + i * 2;
                mesh.AddTriangle(edge - 2, edge, edge - 1);
                mesh.AddTriangle(edge, edge + 1, edge - 1);
            }
        }

        private void AddQuad(VertexHelper mesh, float left, float bottom, float right, float top)
        {
            int first = mesh.currentVertCount;
            Color tint = reviewing || dimmed ? reviewMaskColor : color;
            mesh.AddVert(new Vector3(left, bottom), tint, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), tint, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
