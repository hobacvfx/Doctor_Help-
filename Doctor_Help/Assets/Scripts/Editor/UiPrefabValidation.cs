using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp.Editor
{
    public static class UiPrefabValidation
    {
        public static int Run()
        {
            int checks = 0;
            foreach (string name in new[] { "OpenButton", "GivePillsButton", "GiveButton", "PillOptionButton", "SelectPillPopup", "ResultsPopup" })
            {
                string path = $"Assets/Resources/UI/{name}.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var text in root.GetComponentsInChildren<Text>(true))
                    {
                        Require(text.font != null && !string.IsNullOrWhiteSpace(text.text), name + " has a missing font or label");
                        Require(text.rectTransform.rect.width > 0 && text.rectTransform.rect.height >= text.preferredHeight,
                            name + " clips its text: " + text.name);
                    }
                    foreach (var button in root.GetComponentsInChildren<Button>(true))
                    {
                        Require(button.gameObject != null && button.targetGraphic != null &&
                            button.GetComponentInChildren<Text>(true) != null, name + " has an invalid button");
                        button.interactable = false;
                        button.interactable = true;
                    }
                    var popup = root.GetComponent<SelectPillPopup>();
                    if (popup != null)
                    {
                        Require(popup.IsConfigured, "Popup references are not valid hierarchy components");
                        var serialized = new SerializedObject(popup);
                        var give = (Button)serialized.FindProperty("giveButton").objectReferenceValue;
                        Require(give.gameObject.activeSelf, "Give must be visible even before medicine selection");
                        int given = 0;
                        popup.GiveRequested += () => given++;
                        for (int pass = 0; pass < 2; pass++)
                        {
                            popup.ResetSelection();
                            Require(!give.interactable && popup.SelectedMedicineId == null, "Popup must reset selection");
                            var options = serialized.FindProperty("options");
                            for (int i = 0; i < options.arraySize; i++)
                            {
                                var entry = options.GetArrayElementAtIndex(i);
                                var button = (Button)entry.FindPropertyRelative("button").objectReferenceValue;
                                string id = entry.FindPropertyRelative("medicineId").stringValue;
                                button.onClick.Invoke();
                                Require(popup.SelectedMedicineId == id && give.interactable, "Medicine must enable Give");
                            }
                            give.onClick.Invoke();
                            Require(given == pass + 1, "Reopening popup must not multiply Give callbacks");
                        }
                    }
                    checks++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Debug.Log("UI_PREFAB_VALIDATION_PASSED: labels, fonts, real button components, selection and Give; 6 prefabs.");
            return checks;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("UI prefab validation failed: " + message);
        }
    }
}
