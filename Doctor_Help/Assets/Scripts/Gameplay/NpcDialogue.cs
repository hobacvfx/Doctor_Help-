using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class NpcDialogue : MonoBehaviour
    {
        private const float StreetOutburstLift = 2.7f;
        private const double BrainRotInterjectionChance = .65;
        private static readonly string[] BrainRotInterjections =
        {
            "prum pa pudim param pa",
            "croco mama cromaco",
            "brum tiki vado fla",
            "plom pa nivi tum",
            "krum ta vello pram",
            "fompu riki bom ba",
            "dri da melum pash",
            "puffali rom pom fe"
        };

        [SerializeField] private GameObject chatBox;
        [SerializeField] private AnimationClip chatBoxClip;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private GameObject nextObject;
        [SerializeField] private NpcLipAnimation lips;
        [SerializeField, Min(.1f)] private float charactersPerSecond = 30;
        [SerializeField, Min(0)] private float ambientHoldSeconds = 5;
        [SerializeField] private string medicineThanks = "Thank you!";
        [SerializeField] private bool fitInViewport = true;
        [SerializeField, Min(0)] private float screenPadding = 16;

        private enum Phase { Hidden, Opening, Typing, WaitingForNext, Holding, Closing }
        private Phase phase;
        private SpriteRenderer boxRenderer;
        private SpriteRenderer head;
        private Headbar headbar;
        // The face edge is sampled once per opening, in the NPC root's space, so idle bobbing
        // or bigger body animations never move or re-wrap the bubble.
        private Vector3? speakerEdge;
        private DialogueBubbleLayout layout;
        private string[] lines = Array.Empty<string>();
        private string ambientLine;
        private bool streetOutburst;
        private int lineIndex;
        private int characterCount;
        private float revealed;
        private float animationTime;
        private float holdRemaining;

        internal bool IsShowing => phase != Phase.Hidden;
        internal bool TryGetClueText(out TMP_Text text)
        {
            text = dialogueText;
            return ambientLine == null && text != null && text.gameObject.activeInHierarchy &&
                (phase == Phase.Typing || phase == Phase.WaitingForNext);
        }
        // Patient lines and complaints happen in the Office; ambient lines and outbursts in walking scenes.
        private bool IsExamination => ambientLine == null || patientComplaint;

        public void Initialize(NpcDialogueAsset dialogue, bool addBrainRotInterjections = false)
        {
            HideBox();
            lines = ParseLines(dialogue != null ? dialogue.Content : null);
            if (addBrainRotInterjections) InsertBrainRotInterjections(lines);
            lineIndex = 0;
            if (lines.Length > 0 && (!HasPresentation() || nextObject == null))
                throw new InvalidOperationException("NPC dialogue requires a ChatBox clip, text, lips and Next button.");
        }

        private static string[] ParseLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            text = text.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
            // A final newline terminates the last line without adding a turn.
            if (text.EndsWith("\n", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);
            return text.Split('\n');
        }

        private static void InsertBrainRotInterjections(string[] turns)
        {
            var eligible = new List<(int Turn, int Position)>();
            for (int i = 0; i < turns.Length; i++)
            {
                string turn = turns[i];
                int position = -1;
                int closest = int.MaxValue;
                for (int gap = 1; gap < turn.Length - 1; gap++)
                {
                    if (!char.IsWhiteSpace(turn[gap]) || !char.IsLetterOrDigit(turn[gap - 1])) continue;
                    int next = gap + 1;
                    while (next < turn.Length && char.IsWhiteSpace(turn[next])) next++;
                    int distance = Math.Abs(gap - turn.Length / 2);
                    if (next < turn.Length && char.IsLetterOrDigit(turn[next]) && distance < closest)
                    {
                        position = gap;
                        closest = distance;
                    }
                    gap = next - 1;
                }
                if (position >= 0) eligible.Add((i, position));
            }

            if (eligible.Count == 0) return;
            // Keep dialogue variation independent of spawn and treatment random rolls.
            var random = new System.Random();
            int guaranteed = random.Next(eligible.Count);
            int phraseIndex = random.Next(BrainRotInterjections.Length);
            for (int i = 0; i < eligible.Count; i++)
            {
                if (i != guaranteed && random.NextDouble() >= BrainRotInterjectionChance) continue;
                var (turn, position) = eligible[i];
                turns[turn] = turns[turn].Insert(position, $" — {BrainRotInterjections[phraseIndex]} —");
                phraseIndex = (phraseIndex + random.Next(1, BrainRotInterjections.Length)) % BrainRotInterjections.Length;
            }
        }

        public void BeginAfterArrival()
        {
            if (!IsShowing && lines.Length > 0 && isActiveAndEnabled)
                OpenBox();
        }

        internal void SpeakAmbient(string line)
        {
            BeginAmbient(line, false);
        }

        internal void SpeakStreetOutburst(string line)
        {
            BeginAmbient(line, true);
        }

        internal void SpeakPatientComplaint(string line)
        {
            HideBox();
            patientComplaint = true;
            BeginAmbient(line, false);
        }
        internal void ThankForMedicine() => SpeakPatientComplaint(medicineThanks);

        // Scripted line (end-of-day cutscene): types the line, holds it for `holdSeconds`, then closes.
        internal void SpeakFor(string line, float holdSeconds)
        {
            HideBox();
            customHold = Mathf.Max(0f, holdSeconds);
            BeginAmbient(line, false);
            if (!IsShowing) customHold = null;
        }
        internal bool IsHolding => phase == Phase.Holding;
        internal TMP_Text Text => dialogueText;
        private float? customHold;
        private bool patientComplaint;

        private void BeginAmbient(string line, bool outburst)
        {
            if (IsShowing || !isActiveAndEnabled || string.IsNullOrWhiteSpace(line))
                return;
            if (!HasPresentation())
            {
                Debug.LogError("Ambient NPC dialogue requires a ChatBox clip, text and lips.", this);
                return;
            }

            if (outburst && !chatBox.TryGetComponent<SortingGroup>(out _))
            {
                var sorting = chatBox.AddComponent<SortingGroup>();
                sorting.sortAtRoot = true;
                sorting.sortingOrder = 22;
            }
            streetOutburst = outburst;
            ambientLine = line;
            OpenBox();
        }

        private bool HasPresentation() =>
            chatBox != null && chatBoxClip != null && dialogueText != null && lips != null && lips.IsConfigured;

        private void OpenBox()
        {
            phase = Phase.Opening;
            speakerEdge = null;
            // Size the bubble for its first line before it opens so it does not jump when text appears.
            layout?.SetLine(ambientLine ?? lines[lineIndex], ambientLine == null);
            chatBox.SetActive(true);
            // Measuring needs an initialized TMP component; empty text stays invisible while the bubble opens.
            dialogueText.text = string.Empty;
            dialogueText.gameObject.SetActive(true);
            SampleBox(0f);
        }

        private void StartLine(string line)
        {
            layout?.SetLine(line, ambientLine == null);
            dialogueText.text = line;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();
            characterCount = dialogueText.textInfo.characterCount;
            revealed = 0;
            phase = Phase.Typing;
            if (characterCount > 0)
                lips.SetTalking(true);
            else
                CompleteLine();
        }

        private void Update()
        {
            switch (phase)
            {
                case Phase.Opening:
                    SampleBox(Mathf.Min(chatBoxClip.length, animationTime + Time.deltaTime));
                    if (animationTime >= chatBoxClip.length)
                    {
                        dialogueText.gameObject.SetActive(true);
                        if (ambientLine == null) nextObject.SetActive(true);
                        StartLine(ambientLine ?? lines[lineIndex]);
                    }
                    break;
                case Phase.Typing:
                    revealed += Time.deltaTime * charactersPerSecond;
                    dialogueText.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(revealed));
                    if (revealed >= characterCount) CompleteLine();
                    break;
                case Phase.Holding:
                    holdRemaining -= Time.deltaTime;
                    if (holdRemaining <= 0) CloseBox();
                    break;
                case Phase.Closing:
                    SampleBox(Mathf.Max(0, animationTime - Time.deltaTime));
                    if (animationTime <= 0) HideBox();
                    break;
            }
        }

        private void SampleBox(float time)
        {
            animationTime = time;
            if (layout != null) layout.Sample(time);
            else chatBoxClip.SampleAnimation(chatBox, time);
        }

        private void LateUpdate()
        {
            if (!IsShowing || layout == null) return;
            var camera = Camera.main;
            if (camera == null) return;
            bool examination = IsExamination;
            Vector3 anchor = streetOutburst ? layout.TailAnchor + Vector3.up * StreetOutburstLift : layout.TailAnchor;
            float rightLimit = examination ? SpeakerLeftEdge(camera) : float.PositiveInfinity;
            if (!layout.Apply(camera, anchor,
                    examination ? DialogueTextSize.Examination : DialogueTextSize.Walking, rightLimit, screenPadding)) return;
            // Scripted cutscene lines (customHold) are kept on screen like outbursts and complaints.
            if ((ambientLine != null && !streetOutburst && !patientComplaint && customHold == null) || !fitInViewport) return;

            // Keep the dialogue within the safe area; width is already limited by wrapping.
            var box = chatBox.transform;
            var view = camera.pixelRect;
            var safe = ScreenSafeArea.Current;
            float left = Mathf.Max(view.xMin, safe.xMin) + screenPadding;
            float right = Mathf.Min(view.xMax, safe.xMax) - screenPadding;
            float bottom = Mathf.Max(view.yMin, safe.yMin) + screenPadding;
            float top = Mathf.Min(view.yMax, safe.yMax, HudBottom(camera)) - screenPadding;
            if (right <= left || top <= bottom) return;
            var bounds = boxRenderer.bounds;
            Vector3 min = camera.WorldToScreenPoint(bounds.min), max = camera.WorldToScreenPoint(bounds.max);
            float width = max.x - min.x, height = max.y - min.y;
            if (width <= .01f || height <= .01f) return;
            // Shrinking is only a last resort for very long turns.
            float factor = Mathf.Min(1, (right - left) / width, (top - bottom) / height);
            if (factor < 1) box.localScale *= factor;
            bounds = boxRenderer.bounds;
            min = camera.WorldToScreenPoint(bounds.min); max = camera.WorldToScreenPoint(bounds.max);
            float dx = min.x < left ? left - min.x : max.x > right ? right - max.x : 0;
            float dy = min.y < bottom ? bottom - min.y : max.y > top ? top - max.y : 0;
            if (dx == 0 && dy == 0) return;
            var point = camera.WorldToScreenPoint(box.position);
            box.position += camera.ScreenToWorldPoint(point + new Vector3(dx, dy, 0)) - camera.ScreenToWorldPoint(point);
        }

        // Screen x the bubble must stay left of; it may cover hair at the head's left edge, never the face.
        private float SpeakerLeftEdge(Camera camera)
        {
            if (speakerEdge == null)
            {
                if (head == null || !head.isVisible) return float.PositiveInfinity;
                var bounds = head.bounds;
                float edge = bounds.min.x + bounds.size.x * DialogueTextStyle.Current.FaceOverlap;
                speakerEdge = transform.InverseTransformPoint(new Vector3(edge, bounds.center.y, bounds.center.z));
            }
            return camera.WorldToScreenPoint(transform.TransformPoint(speakerEdge.Value)).x;
        }

        // Bubbles stay under the scene's Headbar so the gold/day bar never covers dialogue.
        private float HudBottom(Camera camera)
        {
            if (headbar == null || !headbar.isActiveAndEnabled || headbar.gameObject.scene != gameObject.scene)
                headbar = FindAnyObjectByType<Headbar>();
            if (headbar == null || !headbar.isActiveAndEnabled || !(headbar.transform is RectTransform rect))
                return float.PositiveInfinity;
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
            foreach (var corner in corners)
            {
                float y = RectTransformUtility.WorldToScreenPoint(uiCamera, corner).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }
            // Ignore a Headbar placed on a full-screen container; only a top strip should push bubbles down.
            return top - bottom < camera.pixelHeight * .3f ? bottom : float.PositiveInfinity;
        }

        private SpriteRenderer FindHead()
        {
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name == "Head" && !renderer.transform.IsChildOf(chatBox.transform))
                    return renderer;
            return null;
        }

        private void CompleteLine()
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
            lips.SetTalking(false);
            if (ambientLine == null)
                phase = Phase.WaitingForNext;
            else
            {
                holdRemaining = customHold ?? (streetOutburst || patientComplaint ? 1f : ambientHoldSeconds);
                phase = Phase.Holding;
            }
        }

        public void Next()
        {
            if (ambientLine != null) return;
            if (phase == Phase.Typing)
                CompleteLine();
            else if (phase == Phase.WaitingForNext)
            {
                lineIndex = (lineIndex + 1) % lines.Length;
                StartLine(lines[lineIndex]);
            }
        }

        public void CloseBox()
        {
            if (!IsShowing || phase == Phase.Closing) return;
            HideContents();
            phase = Phase.Closing;
        }

        private void HideContents()
        {
            if (lips != null) lips.SetTalking(false);
            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
                dialogueText.gameObject.SetActive(false);
            }
            if (nextObject != null) nextObject.SetActive(false);
        }

        private void HideBox()
        {
            phase = Phase.Hidden;
            customHold = null;
            ambientLine = null;
            streetOutburst = false;
            patientComplaint = false;
            HideContents();
            if (chatBox != null) chatBox.SetActive(false);
        }

        private void Awake()
        {
            if (chatBox != null)
            {
                boxRenderer = chatBox.GetComponent<SpriteRenderer>();
                layout = DialogueBubbleLayout.Create(chatBox, chatBoxClip, dialogueText, nextObject);
                head = FindHead();
            }
            HideBox();
        }

        private void OnDisable() => HideBox();
        private void OnDestroy() => layout?.Dispose();
    }
}

