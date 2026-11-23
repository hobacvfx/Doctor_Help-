using UnityEngine;
using UnityEngine.Rendering;

namespace DoctorHelp
{
    // Created before any scene, including when playing Office directly in the Editor.
    internal sealed class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 200;
            OnDemandRendering.renderFrameInterval = 1;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;

            bool preview = false, tutorialPreview = false;
#if UNITY_EDITOR
            tutorialPreview = TutorialPreviewSession.IsActive;
            preview = !tutorialPreview && UnityEditor.SessionState.GetBool("DoctorHelp.PlayerHomePreview", false);
#endif

            var root = new GameObject("Game Services");
            DontDestroyOnLoad(root);
            root.AddComponent<PlayerProfileService>();
            root.AddComponent<BasementDefenseService>(); // Needs the loaded profile; shows nothing itself.
            root.AddComponent<GameAudio>();
            root.AddComponent<GameBootstrap>();
            if (!preview && !tutorialPreview)
            {
                root.AddComponent<DayEndInterstitialService>();
                root.AddComponent<FortuneWheelRewardedAd>();
                root.AddComponent<DoctorHelpFirebaseService>();
            }
            root.AddComponent<DailyGameLoop>().SkipOnboarding = preview;
            root.AddComponent<FirstRunTutorial>();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) return;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 200;
        }
    }
}
