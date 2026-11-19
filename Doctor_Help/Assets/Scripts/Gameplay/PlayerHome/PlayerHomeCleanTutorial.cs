using UnityEngine;

namespace DoctorHelp
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class PlayerHomeCleanTutorial : MonoBehaviour
    {
        [SerializeField] private PlayerHomeMovement player;
        [SerializeField] private PlayerHomeElevator elevator;
        [SerializeField] private PlayerHomeDirt dirt;
        [SerializeField] private PlayerHomeCleaning cleaning;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private TutorialOverlay overlay;
        [SerializeField] private TutorialStep explanation = new TutorialStep
        {
            Action = TutorialAction.ExplainStain,
            Instruction = "Wall stains lower your clinic's star rating. Clean them to reduce the penalty."
        };
        [SerializeField] private TutorialStep cleanPrompt = new TutorialStep
        {
            Action = TutorialAction.SelectClean,
            Instruction = "Tap Clean to remove the stain.",
            HandOffset = new Vector2(0, 35)
        };

        private ClinicWallStain stain;
        private bool waitingForClean;
        private PlayerProfileService profile;
        private DailyGameLoop loop;

        private void Awake()
        {
            profile = PlayerProfileService.Instance;
            loop = profile != null ? profile.GetComponent<DailyGameLoop>() : null;
        }

        private void OnEnable()
        {
            if (cleaning != null) cleaning.Started += OnCleaningStarted;
        }

        private void LateUpdate()
        {
            if (overlay == null || player == null || elevator == null || dirt == null || cleaning == null ||
                profile == null || !profile.IsLoaded || profile.CurrentDay < 3 ||
                !ClinicShopPopup.CanInteract(loop))
            {
                Finish();
                return;
            }

            if (stain == null) TryBegin();
            if (stain == null) return;
            if (!dirt.TryGetStainPosition(stain, out _))
            {
                Finish();
                return;
            }

            if (!waitingForClean && overlay.ReviewConfirmed)
            {
                waitingForClean = true;
                cleaning.GuidedStain = stain;
                profile.SetClean(true);
            }

            if (waitingForClean)
            {
                if (cleaning.Button == null || !cleaning.Button.gameObject.activeInHierarchy) return;
                var rect = (RectTransform)cleaning.Button.transform;
                Vector2 min = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.min));
                Vector2 max = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.max));
                var buttonBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                overlay.Present(buttonBounds, cleanPrompt, readingArea: buttonBounds, handScale: .65f);
            }
            else if (dirt.TryGetScreenBounds(stain, sceneCamera, out var bounds))
                overlay.Present(bounds, explanation, showHand: false);
        }

        private void TryBegin()
        {
            if (profile.Clean || !player.IsWalkingManually) return;
            stain = dirt.FindVisibleStain(sceneCamera, elevator.PlayerFloor(), player.Position.x);
            if (stain != null) player.SetTutorialLocked(true);
        }

        private void OnCleaningStarted(ClinicWallStain target)
        {
            if (waitingForClean && target == stain) Finish();
        }

        private void Finish()
        {
            if (stain == null) return;
            stain = null;
            waitingForClean = false;
            if (cleaning != null) cleaning.GuidedStain = null;
            if (overlay != null) overlay.gameObject.SetActive(false);
            if (player != null) player.SetTutorialLocked(false);
        }

        private void OnDisable()
        {
            if (cleaning != null) cleaning.Started -= OnCleaningStarted;
            Finish();
        }
    }
}
