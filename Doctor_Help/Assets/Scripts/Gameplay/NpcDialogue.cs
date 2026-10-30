using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class NpcDialogue : MonoBehaviour
    {
        [SerializeField] private Animator patientAnimator;
        [SerializeField] private GameObject chatBox;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private GameObject nextObject;
        [SerializeField] private NpcLipAnimation lips;
        [SerializeField, Min(.1f)] private float charactersPerSecond = 30;
        [SerializeField] private bool fitInViewport = true;
        [SerializeField, Range(.1f, 1f)] private float maximumViewportWidth = .38f;
        [SerializeField, Min(0)] private float screenPadding = 16;
        private SpriteRenderer boxRenderer;
        private string[] lines = Array.Empty<string>();
        private Coroutine opening;
        private bool started;
        private float revealed;
        private int characterCount;
        private int dialogueLayer = -1;
        private static readonly int OpenState = Animator.StringToHash("Dialogue.ChatBox_Open");
        private static readonly int ClosedState = Animator.StringToHash("Dialogue.Closed");
        private bool IsOpening => opening != null;
        private bool IsTyping { get; set; }
        private int CurrentLineIndex { get; set; }

        public void Initialize(NpcDialogueAsset dialogue)
        {
            dialogueLayer = patientAnimator != null ? patientAnimator.GetLayerIndex("Dialogue") : -1;
            StopDialogue(); started = false; CurrentLineIndex = 0;
            lines = ParseLines(dialogue != null ? dialogue.Content : null);
            if (lines.Length == 0) return;
            if (chatBox == null || dialogueText == null || nextObject == null || lips == null || !lips.IsConfigured ||
                dialogueLayer < 0 || !patientAnimator.HasState(dialogueLayer, OpenState) || !patientAnimator.HasState(dialogueLayer, ClosedState))
                throw new InvalidOperationException("NPC dialogue references or Dialogue animator states are missing.");
        }
        private static string[] ParseLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            text = text.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
            // A conventional final newline terminates the last line, rather than adding an empty turn.
            if (text.EndsWith("\n", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);
            return text.Split('\n');
        }
        public void BeginAfterArrival()
        {
            if (started || lines.Length == 0 || !isActiveAndEnabled) return;
            started = true;
            chatBox.SetActive(true); dialogueText.gameObject.SetActive(false); nextObject.SetActive(false);
            opening = StartCoroutine(OpenChatBox());
        }
        private IEnumerator OpenChatBox()
        {
            patientAnimator.SetLayerWeight(dialogueLayer, 1);
            patientAnimator.Play(OpenState, dialogueLayer, 0);
            // Let the animator evaluate the requested state before testing its completion.
            yield return null;
            while (patientAnimator != null && patientAnimator.isActiveAndEnabled)
            {
                var state = patientAnimator.GetCurrentAnimatorStateInfo(dialogueLayer);
                if (!patientAnimator.IsInTransition(dialogueLayer) && state.fullPathHash == OpenState && state.normalizedTime >= 1) break;
                yield return null;
            }
            opening = null;
            if (patientAnimator == null || !patientAnimator.isActiveAndEnabled) { StopDialogue(); yield break; }
            dialogueText.gameObject.SetActive(true); nextObject.SetActive(true); StartLine();
        }
        private void StartLine()
        {
            dialogueText.text = lines[CurrentLineIndex];
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();
            characterCount = dialogueText.textInfo.characterCount;
            revealed = 0; IsTyping = characterCount > 0;
            lips.SetTalking(IsTyping);
        }
        private void Update()
        {
            if (!IsTyping) return;
            revealed += Time.deltaTime * charactersPerSecond;
            dialogueText.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(revealed));
            if (revealed >= characterCount) CompleteLine();
        }
        private void LateUpdate()
        {
            if (!fitInViewport || chatBox == null || !chatBox.activeInHierarchy) return;
            var camera = Camera.main;
            if (camera == null) return;
            if (boxRenderer == null) boxRenderer = chatBox.GetComponent<SpriteRenderer>();
            if (boxRenderer == null) return;
            var view = camera.pixelRect;
            var safe = Screen.safeArea;
            float left = Mathf.Max(view.xMin, safe.xMin) + screenPadding;
            float right = Mathf.Min(view.xMax, safe.xMax) - screenPadding;
            float bottom = Mathf.Max(view.yMin, safe.yMin) + screenPadding;
            float top = Mathf.Min(view.yMax, safe.yMax) - screenPadding;
            if (right <= left || top <= bottom) return;
            var bounds = boxRenderer.bounds;
            Vector3 min = camera.WorldToScreenPoint(bounds.min), max = camera.WorldToScreenPoint(bounds.max);
            float width = max.x - min.x, height = max.y - min.y;
            if (width <= .01f || height <= .01f) return;
            float factor = Mathf.Min(1, (right - left) * maximumViewportWidth / width, (top - bottom) / height);
            if (factor < 1) chatBox.transform.localScale *= factor;
            bounds = boxRenderer.bounds;
            min = camera.WorldToScreenPoint(bounds.min); max = camera.WorldToScreenPoint(bounds.max);
            float dx = min.x < left ? left - min.x : max.x > right ? right - max.x : 0;
            float dy = min.y < bottom ? bottom - min.y : max.y > top ? top - max.y : 0;
            if (dx == 0 && dy == 0) return;
            var point = camera.WorldToScreenPoint(chatBox.transform.position);
            chatBox.transform.position += camera.ScreenToWorldPoint(point + new Vector3(dx, dy, 0)) - camera.ScreenToWorldPoint(point);
        }
        private void CompleteLine()
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
            IsTyping = false; lips.SetTalking(false);
        }
        public void Next()
        {
            if (!started || IsOpening || lines.Length == 0 || !chatBox.activeInHierarchy || !nextObject.activeInHierarchy) return;
            if (IsTyping) { CompleteLine(); return; }
            CurrentLineIndex = (CurrentLineIndex + 1) % lines.Length;
            StartLine();
        }
        public void StopDialogue()
        {
            if (opening != null) StopCoroutine(opening);
            opening = null; IsTyping = false;
            if (lips != null) lips.SetTalking(false);
            if (dialogueText != null) { dialogueText.text = ""; dialogueText.gameObject.SetActive(false); }
            if (nextObject != null) nextObject.SetActive(false);
            if (chatBox != null) chatBox.SetActive(false);
            if (patientAnimator != null && patientAnimator.isInitialized && dialogueLayer >= 0 && patientAnimator.HasState(dialogueLayer, ClosedState))
            {
                patientAnimator.SetLayerWeight(dialogueLayer, 0);
                patientAnimator.Play(ClosedState, dialogueLayer, 0);
            }
        }
        private void Awake()
        {
            dialogueLayer = patientAnimator != null ? patientAnimator.GetLayerIndex("Dialogue") : -1;
            StopDialogue();
        }
        private void OnDisable() => StopDialogue();
    }
}






