using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class SettingsPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel, confirmation;
        [SerializeField] private Slider music, effects;
        [SerializeField] private Text musicPercent, effectsPercent, confirmationMessage;
        [SerializeField] private Button close, requestReset, cancelReset, closeConfirmation, confirmReset;

        private DailyGameLoop loop;
        private string warning;
        private bool resetting, switchingPanel;

        private void Awake() => warning = confirmationMessage.text;

        private void OnEnable()
        {
            music.onValueChanged.AddListener(SetMusic);
            effects.onValueChanged.AddListener(SetEffects);
            close.onClick.AddListener(Close);
            requestReset.onClick.AddListener(ShowConfirmation);
            cancelReset.onClick.AddListener(ShowSettings);
            closeConfirmation.onClick.AddListener(ShowSettings);
            confirmReset.onClick.AddListener(ResetProfile);
        }

        public void Open()
        {
            PopupAnimation.Show(gameObject);
            if (loop == null) loop = FindFirstObjectByType<DailyGameLoop>();
            var audio = GameAudio.Instance;
            music.SetValueWithoutNotify(audio != null ? audio.MusicVolume : 1);
            effects.SetValueWithoutNotify(audio != null ? audio.EffectsVolume : 1);
            music.interactable = effects.interactable = audio != null;
            musicPercent.text = Percent(music.value);
            effectsPercent.text = Percent(effects.value);
            confirmation.SetActive(false);
            panel.SetActive(true);
        }

        public void Close() => PopupAnimation.Hide(gameObject);

        private void OnDisable()
        {
            music.onValueChanged.RemoveListener(SetMusic);
            effects.onValueChanged.RemoveListener(SetEffects);
            close.onClick.RemoveListener(Close);
            requestReset.onClick.RemoveListener(ShowConfirmation);
            cancelReset.onClick.RemoveListener(ShowSettings);
            closeConfirmation.onClick.RemoveListener(ShowSettings);
            confirmReset.onClick.RemoveListener(ResetProfile);
            GameAudio.Instance?.SavePreferences();
        }

        private void Update()
        {
            bool canReset = !resetting && loop != null && loop.CanResetProfile;
            requestReset.interactable = confirmReset.interactable = canReset;
        }

        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100)}%";

        private void SetMusic(float value)
        {
            GameAudio.Instance?.SetMusicVolume(value);
            musicPercent.text = Percent(value);
        }

        private void SetEffects(float value)
        {
            GameAudio.Instance?.SetEffectsVolume(value);
            effectsPercent.text = Percent(value);
        }

        private void ShowSettings() => SwitchPanel(false);
        private void ShowConfirmation() => SwitchPanel(true);

        private async void SwitchPanel(bool confirm)
        {
            if (switchingPanel) return;
            switchingPanel = true;
            try
            {
                await PopupAnimation.HideAsync(gameObject);
                if (this == null) return;
                if (confirm) confirmationMessage.text = warning;
                panel.SetActive(!confirm);
                confirmation.SetActive(confirm);
                PopupAnimation.Show(gameObject);
            }
            finally { switchingPanel = false; }
        }

        private async void ResetProfile()
        {
            if (resetting || loop == null || !loop.CanResetProfile) return;
            resetting = true;
            confirmReset.interactable = cancelReset.interactable = closeConfirmation.interactable = false;
            try
            {
                string error = await loop.ResetProfileAsync();
                if (this == null) return;
                if (error == null) Close();
                else confirmationMessage.text = error;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (this != null) confirmationMessage.text = "Could not restart the game. Please try again.";
            }
            finally
            {
                if (this != null)
                {
                    resetting = false;
                    cancelReset.interactable = closeConfirmation.interactable = true;
                }
            }
        }
    }
}
