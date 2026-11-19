using UnityEditor;
using UnityEditor.SceneManagement;

namespace DoctorHelp.Editor
{
    [InitializeOnLoad]
    internal static class HomePlayModeStart
    {
        private const string PlayerHomePath = "Assets/Scenes/Player_Home.unity";
        private const string PreviewKey = "DoctorHelp.PlayerHomePreview";

        static HomePlayModeStart()
        {
            SetStartScene();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Player Home/Preview")]
        private static void Preview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SessionState.SetBool(PreviewKey, true);
            SetStartScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Tutorial/Preview First Run")]
        private static void PreviewTutorial()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            TutorialPreviewSession.Begin();
            SetStartScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Tutorial/Preview First Run", true)]
        private static bool CanPreviewTutorial() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode)
                return;

            SessionState.EraseBool(PreviewKey);
            TutorialPreviewSession.End();
            SetStartScene();
        }

        private static void SetStartScene()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayerHomePath);
        }
    }
}
