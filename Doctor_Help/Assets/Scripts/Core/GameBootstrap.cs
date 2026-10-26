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

            var root = new GameObject("Game Services");
            DontDestroyOnLoad(root);
            root.AddComponent<PlayerProfileService>();
            root.AddComponent<GameBootstrap>();
            root.AddComponent<DailyGameLoop>();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) return;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 200;
        }
    }
}
