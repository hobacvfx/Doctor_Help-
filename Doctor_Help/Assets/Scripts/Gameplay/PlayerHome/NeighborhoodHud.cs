using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class NeighborhoodHud : MonoBehaviour
    {
        [SerializeField] private GameObject rankingsPanel;
        [SerializeField] private Text mainObjectiveText, sideObjectiveTitle, sideObjectiveText;
        [SerializeField] private string clearFirstDay, makeGold, upgradeReceptionist, leadRankings;
        [SerializeField] private Button rankingsButton;
        [SerializeField] private Image rankingsButtonBackground;
        [SerializeField] private Color openButtonTint;
        [SerializeField] private ClinicRankingsCatalog catalog;
        [SerializeField] private RectTransform rankingRows;
        [SerializeField] private ClinicRankingRow rowPrefab;

        private static bool rankingsOpen;
        private readonly List<ClinicRankingRow> rows = new List<ClinicRankingRow>();
        private PlayerProfileService profile;
        private Color closedButtonTint;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRankingsVisibility() => rankingsOpen = false;

        private void Awake()
        {
            closedButtonTint = rankingsButtonBackground.color;
        }

        private void OnEnable()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += Refresh;
            RefreshMainObjective();
            RefreshSideObjectives();
            rankingsButton.onClick.AddListener(ToggleRankings);
            ApplyRankingsVisibility();
        }

        private void OnDisable()
        {
            rankingsButton.onClick.RemoveListener(ToggleRankings);
            if (profile != null) profile.Changed -= Refresh;
        }

        public void SetSideObjectives(IEnumerable<string> objectives)
        {
            var lines = new List<string>();
            if (objectives != null)
                foreach (string objective in objectives)
                    if (!string.IsNullOrWhiteSpace(objective)) lines.Add(objective.Trim());

            sideObjectiveTitle.gameObject.SetActive(lines.Count > 0);
            sideObjectiveText.gameObject.SetActive(lines.Count > 0);
            sideObjectiveText.text = string.Join("\n", lines);
        }

        private void Refresh()
        {
            RefreshMainObjective();
            RefreshSideObjectives();
            RefreshRankings();
        }

        private void RefreshSideObjectives()
        {
            if (profile == null || !profile.IsLoaded) return;
            var quest = profile.WalterContract;
            SetSideObjectives(quest.state == WalterQuestState.Active
                ? new[] { $"Walter: Earn {WalterQuest.Target:N0} Gold ({quest.earned:N0}/{WalterQuest.Target:N0})\nDay {profile.CurrentDay - quest.acceptedDay + 1}/{WalterQuest.Days}" }
                : null);
        }

        private void RefreshMainObjective()
        {
            var objective = profile != null && profile.IsLoaded ? profile.CurrentMainObjective : MainObjective.ClearFirstDay;
            mainObjectiveText.text = objective switch
            {
                MainObjective.MakeGold => makeGold,
                MainObjective.UpgradeReceptionist => upgradeReceptionist,
                MainObjective.LeadRankings => leadRankings,
                MainObjective.Completed => "✓ " + leadRankings,
                _ => clearFirstDay
            };
        }

        private void ToggleRankings()
        {
            rankingsOpen = !rankingsPanel.activeSelf;
            ApplyRankingsVisibility();
        }

        private void ApplyRankingsVisibility()
        {
            rankingsPanel.SetActive(rankingsOpen);
            rankingsButtonBackground.color = rankingsOpen ? openButtonTint : closedButtonTint;
            RefreshRankings();
        }

        private void RefreshRankings()
        {
            if (!rankingsPanel.activeSelf || profile == null || !profile.IsLoaded) return;
            var standings = catalog.GetStandings(profile);
            for (int index = 0; index < standings.Count; index++)
            {
                if (index == rows.Count) rows.Add(Instantiate(rowPrefab, rankingRows, false));
                rows[index].Show(index + 1, standings[index]);
                rows[index].gameObject.SetActive(true);
            }
            for (int index = standings.Count; index < rows.Count; index++) rows[index].gameObject.SetActive(false);
        }
    }
}
