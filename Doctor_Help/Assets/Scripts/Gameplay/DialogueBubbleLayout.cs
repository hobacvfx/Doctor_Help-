using System;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DoctorHelp
{
    internal enum DialogueTextSize { Examination, Walking }

    // Sizes a 9-slice speech bubble around its text so every speaker shows the same on-screen text size.
    // The open clip is evaluated through a manual PlayableGraph: AnimationClip.SampleAnimation only works
    // in the Editor unless the target has an Animator with a controller, so builds would skip the pop.
    internal sealed class DialogueBubbleLayout : IDisposable
    {
        private readonly Transform box;
        private readonly SpriteRenderer boxRenderer;
        private readonly TMP_Text text;
        private readonly RectTransform textRect;
        private readonly Transform next;
        private readonly BoxCollider2D nextArea;
        private readonly AnimationClip openClip;
        private Vector3 openedScale = Vector3.one;
        private Vector3 tailAnchor;
        private bool hasOpenedPose;
        private Vector2 pop = Vector2.one;
        private Animator animator;
        private PlayableGraph graph;
        private AnimationClipPlayable clipPlayable;

        private string line = string.Empty;
        private bool hasNext;
        private bool dirty = true;
        private float scale;
        private float maximumWidth;
        private float lineHeight;
        private Vector2 tailOffset;

        // The tail position of the fully opened authored bubble, in the bubble parent's local space.
        internal Vector3 TailAnchor => hasOpenedPose ? tailAnchor : box.localPosition;

        internal static DialogueBubbleLayout Create(GameObject chatBox, AnimationClip openClip, TMP_Text text, GameObject next)
        {
            var renderer = chatBox != null ? chatBox.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null || openClip == null || text == null) return null;
            if (renderer.sprite.border == Vector4.zero)
            {
                Debug.LogError("Speech bubble sprite needs 9-slice borders so the bubble can resize around its text.", renderer.sprite);
                return null;
            }
            return new DialogueBubbleLayout(renderer, openClip, text, next);
        }

        private DialogueBubbleLayout(SpriteRenderer renderer, AnimationClip openClip, TMP_Text text, GameObject next)
        {
            boxRenderer = renderer;
            box = renderer.transform;
            this.text = text;
            textRect = text.rectTransform;

            this.openClip = openClip;

            text.enableAutoSizing = false;
            if (text.fontSizeMax > 0f) text.fontSize = text.fontSizeMax;
            text.margin = Vector4.zero;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;

            if (next != null)
            {
                this.next = next.transform;
                nextArea = next.GetComponent<BoxCollider2D>();
                // Old prefabs squashed Next to cancel the bubble's uneven open scale; the bubble is uniform now.
                var nextScale = this.next.localScale;
                this.next.localScale = new Vector3(nextScale.x, Mathf.Abs(nextScale.x), nextScale.z);
            }

            renderer.drawMode = SpriteDrawMode.Sliced;
        }

        // Poses the bubble at this time of its open clip. The bubble must be active.
        internal void Sample(float time)
        {
            if (!box.gameObject.activeInHierarchy) return;
            if (!hasOpenedPose)
            {
                // Pin the tail where the authored bubble draws it once open, so it still points at the speaker.
                if (!Evaluate(openClip.length)) return;
                openedScale = box.localScale;
                var sprite = boxRenderer.sprite;
                Vector2 tail = (DialogueTextStyle.Current.TailTipPixels - sprite.pivot) / sprite.pixelsPerUnit;
                tailAnchor = box.localPosition + new Vector3(tail.x * openedScale.x, tail.y * openedScale.y, 0f);
                hasOpenedPose = true;
            }
            if (!Evaluate(Mathf.Clamp(time, 0f, openClip.length))) return;
            Vector3 sampled = box.localScale;
            pop = new Vector2(openedScale.x != 0f ? sampled.x / openedScale.x : 1f,
                openedScale.y != 0f ? sampled.y / openedScale.y : 1f);
        }

        private bool Evaluate(float time)
        {
            if (animator == null) animator = box.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                openClip.SampleAnimation(box.gameObject, time);
                return true;
            }
            if (!graph.IsValid())
            {
                if (animator == null) animator = box.gameObject.AddComponent<Animator>();
                animator.applyRootMotion = false;
                graph = PlayableGraph.Create("Speech bubble pop");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "Bubble", animator);
                clipPlayable = AnimationClipPlayable.Create(graph, openClip);
                clipPlayable.SetApplyFootIK(false);
                output.SetSourcePlayable(clipPlayable);
            }
            clipPlayable.SetTime(time);
            graph.Evaluate();
            return true;
        }

        public void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        internal void SetLine(string value, bool showNext)
        {
            line = value ?? string.Empty;
            hasNext = showNext && nextArea != null;
            dirty = true;
        }

        // The last Sample pose becomes a pop multiplier around the tail.
        // rightLimit keeps the whole bubble left of this screen x, so it never covers the speaker's face.
        internal bool Apply(Camera camera, Vector3 anchor, DialogueTextSize size,
            float rightLimit = float.PositiveInfinity, float screenPadding = 0f)
        {
            if (!hasOpenedPose) return false;
            var style = DialogueTextStyle.Current;
            Rect safe = ScreenSafeArea.Current;
            Vector3 parentScale = box.parent != null ? box.parent.lossyScale : Vector3.one;
            float parentX = Mathf.Abs(parentScale.x), parentY = Mathf.Abs(parentScale.y);
            Vector3 anchorWorld = box.parent != null ? box.parent.TransformPoint(anchor) : anchor;
            float worldPerPixel = WorldPerPixel(camera, anchorWorld);
            if (lineHeight <= 0f) lineHeight = text.GetPreferredValues("Hg").y;
            if (!(worldPerPixel > 0f) || !(lineHeight > 0f) || parentX <= 0f || parentY <= 0f) return false;

            float linePixels = safe.width * style.LineHeightFraction(size);
            float targetScale = linePixels * worldPerPixel / (lineHeight * parentY);
            if (!float.IsFinite(targetScale) || targetScale <= 0f) return false;

            float widthPixels = safe.width * style.MaximumBoxWidth(size);
            if (float.IsFinite(rightLimit))
                widthPixels = Mathf.Min(widthPixels, rightLimit - (safe.xMin + screenPadding));
            // Below a few words per line, wrapping stops helping; let the safe-area clamp handle the rest.
            widthPixels = Mathf.Max(widthPixels, linePixels * (style.MinimumTextWidthInLines + 2f));

            if (dirty || Changed(targetScale, scale) || Changed(widthPixels, maximumWidth))
            {
                scale = targetScale;
                maximumWidth = widthPixels;
                Measure(style, widthPixels * worldPerPixel / (scale * parentX));
                dirty = false;
            }

            var visualScale = new Vector2(scale * pop.x, scale * pop.y);
            box.localScale = new Vector3(visualScale.x, visualScale.y, 1f);
            box.localPosition = anchor - (Vector3)Vector2.Scale(tailOffset, visualScale);

            if (float.IsFinite(rightLimit))
            {
                float overlap = camera.WorldToScreenPoint(boxRenderer.bounds.max).x - rightLimit;
                if (overlap > 0f)
                {
                    Vector3 point = camera.WorldToScreenPoint(box.position);
                    box.position += camera.ScreenToWorldPoint(point - new Vector3(overlap, 0f, 0f)) - camera.ScreenToWorldPoint(point);
                }
            }
            return true;
        }

        private void Measure(DialogueTextStyle style, float maximumBoxWidth)
        {
            var sprite = boxRenderer.sprite;
            float pixelsPerUnit = sprite.pixelsPerUnit;
            Vector4 inset = style.ContentInsetPixels / pixelsPerUnit;
            float maximumTextWidth = Mathf.Max(lineHeight * 2f, maximumBoxWidth - inset.x - inset.z);
            Vector2 preferred = text.GetPreferredValues(line, maximumTextWidth, 0f);
            float textWidth = Mathf.Clamp(preferred.x, Mathf.Min(lineHeight * style.MinimumTextWidthInLines, maximumTextWidth), maximumTextWidth);
            float textHeight = Mathf.Max(preferred.y, lineHeight);

            Vector2 nextSize = Vector2.zero;
            float nextRow = 0f;
            float nextScale = next != null ? Mathf.Abs(next.localScale.x) : 1f;
            if (hasNext)
            {
                nextSize = nextArea.size * nextScale;
                nextRow = nextSize.y + lineHeight * style.NextSpacingInLines;
            }

            float contentWidth = Mathf.Max(textWidth, nextSize.x);
            Vector4 border = sprite.border / pixelsPerUnit;
            var size = new Vector2(
                Mathf.Max(contentWidth + inset.x + inset.z, border.x + border.z),
                Mathf.Max(textHeight + nextRow + inset.y + inset.w, border.y + border.w));
            boxRenderer.size = size;

            float textDepth = textRect.localPosition.z;
            textRect.pivot = new Vector2(0f, 1f);
            textRect.sizeDelta = new Vector2(contentWidth, textHeight);
            textRect.localPosition = new Vector3(-size.x * .5f + inset.x, size.y * .5f - inset.y, textDepth);

            if (hasNext)
            {
                var center = new Vector2(size.x * .5f - inset.z - nextSize.x * .5f, -size.y * .5f + inset.w + nextSize.y * .5f);
                Vector2 offset = nextArea.offset * nextScale;
                next.localPosition = new Vector3(center.x - offset.x, center.y - offset.y, next.localPosition.z);
            }

            Vector2 tail = style.TailTipPixels / pixelsPerUnit;
            Vector2 spriteSize = sprite.rect.size / pixelsPerUnit;
            tailOffset = new Vector2(size.x * .5f - (spriteSize.x - tail.x), -size.y * .5f + tail.y);
        }

        private static bool Changed(float value, float previous) => Mathf.Abs(value - previous) > Mathf.Abs(previous) * .01f;

        private static float WorldPerPixel(Camera camera, Vector3 worldPosition)
        {
            Vector3 point = camera.WorldToScreenPoint(worldPosition);
            return Vector3.Distance(camera.ScreenToWorldPoint(point), camera.ScreenToWorldPoint(point + Vector3.up));
        }
    }
}
