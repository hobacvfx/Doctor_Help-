using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoctorHelp
{
    public enum DayStep { Preparation, Examination, SelectingPill, Results }

    // Persistent flow state only. All UI and EventSystems belong to scenes.
    public sealed class DailyGameLoop : MonoBehaviour
    {
        private const string Home = "Home";
        private const string Office = "Office";
        public DayStep Step { get; private set; } = DayStep.Preparation;
        public string SelectedPill { get; private set; }
        private bool transitioning;
        private SceneGameplayUI ui;
        private PlayerProfileService Profile => PlayerProfileService.Instance;

        private IEnumerator Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (SceneManager.GetActiveScene().name != Home)
            {
                transitioning = true;
                yield return SceneManager.LoadSceneAsync(Home);
                transitioning = false;
            }
            else BindSceneUI();
            if (!Profile.IsLoaded)
            {
                ui.ShowError("Không thể tải profile. Kiểm tra Console; dữ liệu cũ được giữ nguyên.");
                yield break;
            }
            ShowPreparation();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindSceneUI();

        private void BindSceneUI()
        {
            ui = FindFirstObjectByType<SceneGameplayUI>();
            if (ui == null) throw new System.InvalidOperationException("Scene is missing its Gameplay Canvas UI.");
            ui.HideAll();
            if (ui.OpenButton != null)
                ui.OpenButton.onClick.AddListener(() => { if (!transitioning) StartCoroutine(OpenOffice()); });
            if (ui.GivePillsButton != null) ui.GivePillsButton.onClick.AddListener(ShowPillSelection);
        }

        private void ShowPreparation()
        {
            Step = DayStep.Preparation;
            SelectedPill = null;
            ui.ShowPreparation(Profile.CurrentDay);
        }

        private IEnumerator OpenOffice()
        {
            if (Step != DayStep.Preparation) yield break;
            transitioning = true;
            ui.HideAll();
            yield return SceneManager.LoadSceneAsync(Office);
            Step = DayStep.Examination;
            if (SpawnPatient()) ui.ShowExamination(Profile.CurrentDay);
            else ui.ShowError("Không thể tạo NPC. Kiểm tra NPC_Position và NpcCatalog.");
            transitioning = false;
        }

        private bool SpawnPatient()
        {
            var anchor = GameObject.Find("NPC_Position");
            var catalog = Resources.Load<NpcCatalog>("NpcCatalog");
            if (anchor == null || catalog == null || catalog.prefabs == null || catalog.prefabs.Length == 0)
            {
                Debug.LogError("Missing NPC_Position or Resources/NpcCatalog.");
                return false;
            }
            var prefab = catalog.prefabs[Random.Range(0, catalog.prefabs.Length)];
            if (prefab == null) { Debug.LogError("NpcCatalog contains an empty prefab reference."); return false; }
            var patient = Instantiate(prefab, anchor.transform, false);
            patient.transform.localPosition = Vector3.zero;
            patient.transform.localRotation = Quaternion.identity;
            patient.name = prefab.name;
            return true;
        }

        private void ShowPillSelection()
        {
            if (transitioning || Step != DayStep.Examination) return;
            Step = DayStep.SelectingPill;
            ui.ShowPillSelection(Profile.CurrentDay);
            ui.PillPopup.Bind(id => SelectedPill = id,
                () => { if (!transitioning && SelectedPill != null) StartCoroutine(FinishDay()); });
        }

        private IEnumerator FinishDay()
        {
            if (Step != DayStep.SelectingPill) yield break;
            transitioning = true;
            if (!Profile.CompleteDay())
            {
                transitioning = false;
                Debug.LogError("Day could not be saved. Press Give again to retry.");
                ui.PillPopup.ShowSaveError();
                yield break;
            }
            Step = DayStep.Results;
            ui.HideAll();
            yield return SceneManager.LoadSceneAsync(Home);
            ui.ShowResults();
            yield return new WaitForSecondsRealtime(3f);
            ShowPreparation();
            transitioning = false;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
