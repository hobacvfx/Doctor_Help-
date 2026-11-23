using System.Collections;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class FortuneWheelEntrance : MonoBehaviour
    {
        [SerializeField] private DeskActionButton actionButton;
        [SerializeField] private FortuneWheelPopup popup;
        private Coroutine pending;
        private void OnEnable() { if (actionButton != null) actionButton.Clicked += Open; }
        private void OnDisable()
        {
            if (actionButton != null) actionButton.Clicked -= Open;
            if (pending != null) StopCoroutine(pending);
            pending = null;
        }
        private void Open()
        {
            if (pending == null && popup != null) pending = StartCoroutine(OpenNextFrame());
        }
        private IEnumerator OpenNextFrame()
        {
            yield return null;
            pending = null;
            popup.Open();
        }
    }
}
