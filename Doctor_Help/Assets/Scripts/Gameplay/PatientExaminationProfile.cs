using System;
using UnityEngine;

namespace DoctorHelp
{
    // The spawned patient's authored identity and examination anchors, separate from appearance.
    [DisallowMultipleComponent]
    public sealed class PatientExaminationProfile : MonoBehaviour
    {
        [SerializeField] private Transform heart;
        [SerializeField, HideInInspector] private NpcProfile profile;
        [SerializeField, HideInInspector] private string npcId;
        [SerializeField, HideInInspector] private bool hasConsultationTemperature;
        [SerializeField, HideInInspector] private float consultationTemperature;
        public Transform Heart => heart;
        public NpcProfile Profile => profile;
        public NpcDefinition Definition => profile != null ? profile.Find(npcId) : null;

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
            // Initialize marks a new consultation, including reuse of a pooled patient.
            hasConsultationTemperature = false;
            consultationTemperature = 0;
        }
    }
}

