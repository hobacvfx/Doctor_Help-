using UnityEditor;
using UnityEditor.SceneManagement;

namespace DoctorHelp.Editor
{
    [InitializeOnLoad]
    internal static class HomePlayModeStart
    {
        static HomePlayModeStart()
        {
            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Home.unity");
        }
    }
}
