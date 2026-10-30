using System;
using UnityEngine;

namespace DoctorHelp
{
    [Serializable]
    public sealed class HeartbeatRhythm
    {
        [SerializeField, Min(1)] private float normalBpm = 60;
        [SerializeField] private string rapidHeartbeatSymptomId;
        [SerializeField] private string slowHeartbeatSymptomId;
        [SerializeField] private string noHeartbeatSymptomId;
        public float NormalBpm => normalBpm;

        public float Resolve(NpcDefinition npc, AllSickness medicalData)
        {
            if (npc == null) throw new InvalidOperationException("Heartbeat requires a valid NPC record.");
            if (medicalData == null || medicalData.SymptomProfile == null)
                throw new InvalidOperationException("Heartbeat requires a valid NPC record and medical catalogue.");
            if (normalBpm <= 0 || float.IsNaN(normalBpm) || float.IsInfinity(normalBpm))
                throw new InvalidOperationException("Normal heartbeat BPM must be finite and positive.");
            var symptoms = medicalData.SymptomProfile;
            if (symptoms.Find(rapidHeartbeatSymptomId) == null || symptoms.Find(slowHeartbeatSymptomId) == null ||
                symptoms.Find(noHeartbeatSymptomId) == null || rapidHeartbeatSymptomId == slowHeartbeatSymptomId ||
                rapidHeartbeatSymptomId == noHeartbeatSymptomId || slowHeartbeatSymptomId == noHeartbeatSymptomId)
                throw new InvalidOperationException("Heartbeat symptom mappings are missing, duplicated or no longer valid.");
            if (!npc.HasSickness) return normalBpm;
            var sickness = medicalData.Find(npc.SicknessId);
            if (sickness == null) throw new InvalidOperationException("NPC references a missing sickness: " + npc.SicknessId);
            bool rapid = false, slow = false, none = false;
            foreach (var id in sickness.SymptomIds)
            {
                if (symptoms.Find(id) == null) throw new InvalidOperationException("Sickness references a missing symptom: " + id);
                rapid |= id == rapidHeartbeatSymptomId;
                slow |= id == slowHeartbeatSymptomId;
                none |= id == noHeartbeatSymptomId;
            }
            if (none) return 0;
            if (rapid && slow) throw new InvalidOperationException("Sickness has conflicting rapid and slow heartbeat symptoms.");
            return normalBpm * (rapid ? 3 : slow ? 0.5f : 1);
        }

        public static float DistanceGain(float distance, float maxDistance)
            => maxDistance > 0 ? Mathf.Clamp01(1 - distance / maxDistance) : 0;
    }
}



