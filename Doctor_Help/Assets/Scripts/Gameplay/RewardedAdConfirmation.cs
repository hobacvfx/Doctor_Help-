using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class RewardedAdConfirmation : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea, panel;
        [SerializeField] private Button yes, no;
        private TaskCompletionSource<bool> pending;

        public async Task<bool> AskAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (pending != null) return false;
            var request = pending = new TaskCompletionSource<bool>();
            yes.onClick.AddListener(Accept);
            no.onClick.AddListener(Decline);
            PopupAnimation.Show(gameObject);
            EventSystem.current?.SetSelectedGameObject(null);
            try
            {
                using (cancellation.Register(() => request.TrySetCanceled()))
                    return await request.Task;
            }
            finally
            {
                if (this != null)
                {
                    yes.onClick.RemoveListener(Accept);
                    no.onClick.RemoveListener(Decline);
                    await PopupAnimation.HideAsync(gameObject);
                    pending = null;
                }
            }
        }

        private void Accept() => pending?.TrySetResult(true);
        private void Decline() => pending?.TrySetResult(false);
        private void OnDisable() => Decline();
        private void OnDestroy() => pending?.TrySetCanceled();
        private void LateUpdate()
        {
            ScreenSafeArea.ApplyTo(safeArea);
            panel.localScale = Vector3.one * Mathf.Min(safeArea.rect.width / 540f, safeArea.rect.height / 960f);
        }
    }
}
