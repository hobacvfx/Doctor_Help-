using System.Collections;
using TMPro;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class GhostDialogue : MonoBehaviour
    {
        [SerializeField] private GameObject chatBox;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private AnimationClip chatBoxClip;
        [SerializeField, Min(0.1f)] private float charactersPerSecond = 30f;
        [SerializeField, Min(0f)] private float holdSeconds = 5f;

        private static readonly string[] Lines =
        {
            "I remember this place.",
            "Why i can't leave this place?",
            "Give me the stone!"
        };

        private Coroutine speech;
        private DialogueBubbleLayout layout;
        internal bool IsSpeaking => speech != null;

        private void Awake()
        {
            if (chatBox != null && dialogueText != null && chatBoxClip != null)
            {
                layout = DialogueBubbleLayout.Create(chatBox, chatBoxClip, dialogueText, null);
                return;
            }

            Debug.LogError("Ghost dialogue requires a chatbox, dialogue text and animation clip.", this);
            enabled = false;
        }

        internal void Speak()
        {
            if (isActiveAndEnabled && !IsSpeaking)
                speech = StartCoroutine(PlaySpeech());
        }

        private IEnumerator PlaySpeech()
        {
            string line = Lines[Random.Range(0, Lines.Length)];
            // Size the bubble for its line before it opens so it does not jump when text appears.
            layout?.SetLine(line, false);
            dialogueText.text = string.Empty;
            chatBox.SetActive(true);
            yield return AnimateBox(opening: true);

            dialogueText.maxVisibleCharacters = 0;
            dialogueText.text = line;
            dialogueText.ForceMeshUpdate();
            int characterCount = dialogueText.textInfo.characterCount;
            float revealed = 0f;
            while (revealed < characterCount)
            {
                yield return null;
                revealed += Time.deltaTime * charactersPerSecond;
                dialogueText.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(revealed));
            }

            yield return new WaitForSeconds(holdSeconds);
            dialogueText.text = string.Empty;
            yield return AnimateBox(opening: false);
            chatBox.SetActive(false);
            speech = null;
        }

        private IEnumerator AnimateBox(bool opening)
        {
            // Sampling leaves the last pose in place and uses the same curve in both directions.
            float duration = chatBoxClip.length;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                SampleBox(opening ? elapsed : duration - elapsed);
                yield return null;
                elapsed += Time.deltaTime;
            }
            SampleBox(opening ? duration : 0f);
        }

        private void SampleBox(float time)
        {
            if (layout != null) layout.Sample(time);
            else chatBoxClip.SampleAnimation(chatBox, time);
        }

        private void LateUpdate()
        {
            if (layout == null || !chatBox.activeSelf) return;
            var camera = Camera.main;
            if (camera != null) layout.Apply(camera, layout.TailAnchor, DialogueTextSize.Walking);
        }

        private void OnDestroy() => layout?.Dispose();

        private void OnDisable()
        {
            if (speech != null)
            {
                StopCoroutine(speech);
                speech = null;
            }
            if (dialogueText != null) dialogueText.text = string.Empty;
            if (chatBox != null) chatBox.SetActive(false);
        }
    }
}
