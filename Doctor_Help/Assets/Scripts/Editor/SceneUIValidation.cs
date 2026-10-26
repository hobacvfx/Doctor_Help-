using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DoctorHelp.Editor
{
    public static class SceneUIValidation
    {
        [MenuItem("Doctor Help/Validate Scene UI")]
        public static void Run()
        {
            foreach (string name in new[] { "Home", "Office" })
            {
                var scene = EditorSceneManager.OpenPreviewScene($"Assets/Scenes/{name}.unity");
                try
                {
                    SceneGameplayUI view = null;
                    int eventSystems = 0;
                    int overlays = 0;
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        var found = root.GetComponentInChildren<SceneGameplayUI>(true);
                        if (found != null)
                        {
                            if (view != null) throw new Exception(name + " contains multiple Gameplay UI views.");
                            view = found;
                        }
                        eventSystems += root.GetComponentsInChildren<EventSystem>(true).Length;
                        overlays += root.GetComponentsInChildren<FpsOverlay>(true).Length;
                    }
                    if (view == null || !view.IsConfigured || view.GetComponent<Canvas>() == null ||
                        view.transform.parent != null || eventSystems != 1 || overlays != 1)
                        throw new Exception(name + " scene UI has missing or duplicate references.");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            Debug.Log("SCENE_UI_VALIDATION_PASSED: Home and Office each own their Canvas, UI, FPS and EventSystem.");
        }
    }
}
