using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    internal sealed class WalterDialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject conversation;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text speech, secondaryLabel, rewardText;
        [SerializeField] private Button next, accept, secondary;
        [SerializeField] private Headbar headbar;
        [SerializeField] private Camera portraitCamera;
        [SerializeField] private Transform portraitStage;
        [SerializeField] private RawImage portrait;
        [SerializeField, Min(1)] private float charactersPerSecond = 30f;
        [SerializeField, TextArea] private string offer;
        [SerializeField, TextArea] private string reminder;
        [SerializeField, TextArea] private string completed;
        [SerializeField, TextArea] private string failed;
        [SerializeField, TextArea] private string thanked;

        private PlayerProfileService profile;
        private WalterQuestState state;
        private string[] lines;
        private int lineIndex;
        private float typed, previousTimeScale;
        private bool speaking, holdsPause;
        private RenderTexture portraitTexture;

        private void OnEnable()
        {
            next.onClick.AddListener(Next);
            accept.onClick.AddListener(Accept);
            secondary.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            next.onClick.RemoveListener(Next);
            accept.onClick.RemoveListener(Accept);
            secondary.onClick.RemoveListener(Close);
            ResumeGameplay();
            portraitCamera.targetTexture = null;
            portrait.texture = null;
            if (portraitTexture != null) Destroy(portraitTexture);
        }

        internal void Show(PlayerProfileService player)
        {
            if (gameObject.activeSelf) return;
            profile = player;
            var quest = player.WalterContract;
            state = quest.state;
            string dialogue = state switch
            {
                WalterQuestState.Available => offer,
                WalterQuestState.Active => string.Format(reminder, quest.earned,
                    WalterQuest.Days - (player.CurrentDay - quest.acceptedDay)),
                WalterQuestState.ReadyToClaim => completed,
                WalterQuestState.Failed => failed,
                _ => thanked
            };
            lines = dialogue.TrimEnd('\r', '\n').Replace("\r\n", "\n").Split('\n');
            previousTimeScale = Time.timeScale;
            holdsPause = true;
            Time.timeScale = 0f;
            gameObject.SetActive(true);
            conversation.SetActive(true);
            UpdatePortraitScale();
            portraitTexture = new RenderTexture(320, 640, 16, RenderTextureFormat.ARGB32);
            portraitCamera.targetTexture = portraitTexture;
            portraitCamera.aspect = (float)portraitTexture.width / portraitTexture.height;
            portrait.texture = portraitTexture;
            rewardText.gameObject.SetActive(false);
            lineIndex = 0;
            BeginLine();
        }

        private void Update()
        {
            ScreenSafeArea.ApplyTo(safeArea);
            if (!speaking) return;
            typed += Time.unscaledDeltaTime * charactersPerSecond;
            var line = lines[lineIndex];
            int length = Mathf.Min(line.Length, (int)typed);
            speech.text = line.Substring(0, length);
            if (length == line.Length) FinishLine();
        }

        private void LateUpdate()
        {
            if (conversation.activeSelf) UpdatePortraitScale();
        }

        private void UpdatePortraitScale()
        {
            // CanvasScaler can resize the overlay hierarchy on high-DPI screens.
            // Keep the portrait rig and camera in the prefab's authored world units.
            Vector3 scale = portraitStage.parent.lossyScale;
            portraitStage.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        }

        private void BeginLine()
        {
            typed = 0f;
            speaking = true;
            speech.text = "";
            next.gameObject.SetActive(true);
            accept.gameObject.SetActive(false);
            secondary.gameObject.SetActive(false);
        }

        private void FinishLine()
        {
            speaking = false;
            speech.text = lines[lineIndex];
            bool last = lineIndex == lines.Length - 1;
            next.gameObject.SetActive(!last);
            accept.gameObject.SetActive(last && (state is WalterQuestState.Available or WalterQuestState.ReadyToClaim));
            secondary.gameObject.SetActive(last && state != WalterQuestState.ReadyToClaim);
            secondaryLabel.text = state == WalterQuestState.Available ? "Decline" : "Close";
        }

        private void Next()
        {
            if (speaking) FinishLine();
            else if (lineIndex + 1 < lines.Length)
            {
                lineIndex++;
                BeginLine();
            }
        }

        private void Accept()
        {
            if (state == WalterQuestState.Available && profile.AcceptWalterQuest()) Close();
            else if (state == WalterQuestState.ReadyToClaim && profile.ClaimWalterReward())
            {
                conversation.SetActive(false);
                ResumeGameplay();
                speaking = false;
                StartCoroutine(FlyReward());
            }
        }

        private IEnumerator FlyReward()
        {
            rewardText.text = $"+{WalterQuest.Reward} Gold";
            rewardText.gameObject.SetActive(true);
            var rect = rewardText.rectTransform;
            Vector3 start = safeArea.TransformPoint(safeArea.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, headbar.GoldTarget.position);
            RectTransformUtility.ScreenPointToWorldPointInRectangle(safeArea, screen, null, out var end);
            const float duration = .75f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                rect.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            rect.position = end;
            yield return null;
            Close();
        }

        private void ResumeGameplay()
        {
            if (!holdsPause) return;
            holdsPause = false;
            Time.timeScale = previousTimeScale;
        }

        private void Close()
        {
            speaking = false;
            gameObject.SetActive(false);
        }
    }
}
