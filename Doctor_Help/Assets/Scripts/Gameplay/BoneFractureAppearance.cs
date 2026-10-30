using System;
using UnityEngine;

namespace DoctorHelp
{
    // Consultation appearance, independent of the shared material and pointer/X-ray state.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PatientExaminationProfile))]
    public sealed class BoneFractureAppearance : MonoBehaviour
    {
        [SerializeField] private Renderer boneRenderer;
        [SerializeField] private string boneFractureSymptomId;
        [SerializeField, HideInInspector] private bool initialized;
        [SerializeField, HideInInspector] private bool hasFracture;
        [SerializeField, HideInInspector] private Vector2 crackOffset;
        private MaterialPropertyBlock properties;
        private static readonly int BoneCrackId = Shader.PropertyToID("_BoneCrack");
        private static readonly int CrackOffsetId = Shader.PropertyToID("_Crack_Offset");

        public void Initialize(NpcDefinition npc, AllSickness medicalData)
        {
            if (boneRenderer == null || boneRenderer.sharedMaterial == null ||
                !boneRenderer.sharedMaterial.HasProperty(BoneCrackId) || !boneRenderer.sharedMaterial.HasProperty(CrackOffsetId))
                throw new InvalidOperationException("Bone requires an assigned renderer with _BoneCrack and _Crack_Offset shader properties.");
            if (npc == null || medicalData == null || medicalData.SymptomProfile == null ||
                string.IsNullOrEmpty(boneFractureSymptomId) || medicalData.SymptomProfile.Find(boneFractureSymptomId) == null)
                throw new InvalidOperationException("Bone fracture requires a valid NPC, medical catalogue and symptom mapping.");
            bool fracture = false;
            if (npc.HasSickness)
            {
                var sickness = medicalData.Find(npc.SicknessId);
                if (sickness == null) throw new InvalidOperationException("NPC references a missing sickness: " + npc.SicknessId);
                foreach (var symptomId in sickness.SymptomIds)
                {
                    if (medicalData.SymptomProfile.Find(symptomId) == null)
                        throw new InvalidOperationException("Sickness references a missing symptom: " + symptomId);
                    fracture |= symptomId == boneFractureSymptomId;
                }
            }
            hasFracture = fracture;
            crackOffset = fracture ? new Vector2(UnityEngine.Random.value, UnityEngine.Random.value) : Vector2.zero;
            initialized = true;
            Apply();
        }

        private void OnEnable() { if (initialized) Apply(); }
        private void Apply()
        {
            if (boneRenderer == null) return;
            properties ??= new MaterialPropertyBlock();
            // Preserve the X-ray controller's properties on this same renderer.
            boneRenderer.GetPropertyBlock(properties);
            properties.SetFloat(BoneCrackId, hasFracture ? 1 : 0);
            properties.SetVector(CrackOffsetId, new Vector4(crackOffset.x, crackOffset.y, 0, 0));
            boneRenderer.SetPropertyBlock(properties);
        }
    }
}

