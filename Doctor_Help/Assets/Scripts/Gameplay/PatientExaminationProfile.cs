using System;
using UnityEngine;

namespace DoctorHelp
{
    // The spawned patient's authored identity and examination anchors, separate from appearance.
    [DisallowMultipleComponent]
    public sealed class PatientExaminationProfile : MonoBehaviour
    {
        [SerializeField] private Transform heart;
        [SerializeField] private SpriteRenderer treasureBody;
        [SerializeField, HideInInspector] private NpcProfile profile;
        [SerializeField, HideInInspector] private string npcId;
        [SerializeField, HideInInspector] private bool hasConsultationTemperature;
        [SerializeField, HideInInspector] private float consultationTemperature;
        public Transform Heart => heart;
        internal SpriteRenderer TreasureBody => treasureBody;
        public NpcProfile Profile => profile;
        private NpcDefinition consultationDefinition;
        public NpcDefinition Definition => consultationDefinition ?? (profile != null ? profile.Find(npcId) : null);
        internal SicknessDefinition Haunting { get; private set; }
        internal bool HauntingCleared { get; set; }
        internal bool CanConfirmWithoutMedicine => HauntingCleared && Definition?.IsHauntedOnly == true;
        internal PatientRockReaction RockReaction { get; set; }
        [SerializeField, HideInInspector] private string consultationId;
        public string ConsultationId => consultationId;

        private const double PatienceDuration = 45;
        private double maximumPatienceSeconds = PatienceDuration;
        private double patienceSeconds;
        private bool patienceRunning;
        public bool HasArrived { get; private set; }
        public float Patience => (float)(100 * patienceSeconds / maximumPatienceSeconds);
        public int PatienceStars => Mathf.Min(5, 1 + Mathf.FloorToInt(Patience / 10f));

        internal void BeginPatience()
        {
            if (HasArrived) return;
            HasArrived = patienceRunning = true;
        }

        internal void StopPatience() => patienceRunning = false;

        internal void AdvancePatience(float seconds)
        {
            if (!patienceRunning) return;
            patienceSeconds = Math.Max(0, patienceSeconds - seconds);
            if (patienceSeconds == 0) StopPatience();
        }

        public float GetConsultationTemperature(TemperatureRules rules)
        {
            if (rules == null || profile == null || Definition == null)
                throw new InvalidOperationException("Temperature requires an initialized patient consultation and rules.");
            if (!hasConsultationTemperature)
            {
                float sampled = rules.Sample(Definition, profile.AllSickness, UnityEngine.Random.value);
                consultationTemperature = sampled;
                hasConsultationTemperature = true;
            }
            return consultationTemperature;
        }

        public void Initialize(NpcProfile source, NpcDefinition definition)
        {
            if (source == null || definition == null || heart == null)
                throw new InvalidOperationException("Patient requires an NPC profile record and an assigned Heart anchor.");
            profile = source;
            npcId = definition.Id;
            bool forceHaunted = PlayerProfileService.Instance?.HauntedTest == true;
            definition = definition.ForConsultation(forceHaunted);
            consultationDefinition = ReferenceEquals(definition, source.Find(npcId)) ? null : definition;
            Haunting = NpcMedicalConditions.Resolve(definition.SicknessIds, source.AllSickness)
                .Contains(NpcMedicalConditions.HauntedSymptom) ? source.AllSickness.Find(NpcMedicalConditions.Haunted) : null;
            HauntingCleared = false;
            RockReaction = null;
            consultationId = Guid.NewGuid().ToString("N");
            // Initialize marks a new consultation, including reuse of a pooled patient.
            HasArrived = patienceRunning = false;
            var playerProfile = PlayerProfileService.Instance;
            int elevatorLevel = playerProfile != null && playerProfile.IsLoaded ? playerProfile.ElevatorLevel : 0;
            maximumPatienceSeconds = PatienceDuration * (10 + elevatorLevel) / 10;
            patienceSeconds = maximumPatienceSeconds;
            hasConsultationTemperature = false;
            consultationTemperature = 0;
            if (TryGetComponent<BoneFractureAppearance>(out var boneAppearance))
                boneAppearance.Initialize(definition, source.AllSickness);
            if (TryGetComponent<WormInfestationAppearance>(out var wormAppearance))
                wormAppearance.Initialize(definition, source.AllSickness);
            if (TryGetComponent<OrganAbnormalityAppearance>(out var organAppearance))
                organAppearance.Initialize(definition, source.AllSickness);
            if (TryGetComponent<SymptomPrefabAppearance>(out var symptomAppearance))
                symptomAppearance.Initialize(definition, source.AllSickness);
            if (TryGetComponent<NpcDialogue>(out var dialogue))
                dialogue.Initialize(definition.Dialogue, definition.HasBrainRot);
        }
    }
}






