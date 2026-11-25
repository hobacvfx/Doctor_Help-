using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class ClinicNamingView : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Button okayButton;
        [SerializeField] private Text errorText;
        private PlayerProfileService profile;
        private bool confirmed;
        internal bool IsConfigured => safeArea != null && nameInput != null && okayButton != null && errorText != null;

        internal async Task PresentAsync(PlayerProfileService service, CancellationToken token)
        {
            if (!IsConfigured) throw new InvalidOperationException("Clinic naming prefab has missing references.");
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, destroyCancellationToken);
            profile = service;
            confirmed = false;
            nameInput.characterLimit = ClinicNameRules.MaxLength;
            nameInput.SetTextWithoutNotify(service.ClinicName);
            errorText.text = "";
            okayButton.onClick.AddListener(Confirm);
            nameInput.onValueChanged.AddListener(ClearError);
            PopupAnimation.Show(gameObject);
            ScreenSafeArea.ApplyTo(safeArea);
            try
            {
                while (!confirmed) await Awaitable.NextFrameAsync(lifetime.Token);
            }
            finally
            {
                profile = null;
                if (this != null)
                {
                    okayButton.onClick.RemoveListener(Confirm);
                    nameInput.onValueChanged.RemoveListener(ClearError);
                    nameInput.DeactivateInputField();
                    await PopupAnimation.HideAsync(gameObject);
                }
            }
        }

        private void Confirm()
        {
            if (confirmed || profile == null) return;
            if (!profile.TrySetClinicName(nameInput.text, out var error)) { errorText.text = error; return; }
            confirmed = true;
            nameInput.DeactivateInputField();
        }

        private void ClearError(string _) => errorText.text = "";
        private void LateUpdate() => ScreenSafeArea.ApplyTo(safeArea);
    }
}
