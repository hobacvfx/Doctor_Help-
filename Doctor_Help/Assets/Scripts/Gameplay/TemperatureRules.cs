using System;
using UnityEngine;
namespace DoctorHelp
{
    [Serializable]
    public sealed class TemperatureRules
    {
        [SerializeField] private string highTemperatureSymptomId;
        [SerializeField] private string lowTemperatureSymptomId;
        public float Sample(NpcDefinition npc, AllSickness medicalData, float randomUnit)
        {
            if(npc==null || medicalData==null || medicalData.SymptomProfile==null)
                throw new InvalidOperationException("Temperature requires a valid NPC and medical catalogue.");
            var symptoms=medicalData.SymptomProfile;
            if(string.IsNullOrEmpty(highTemperatureSymptomId) || string.IsNullOrEmpty(lowTemperatureSymptomId) ||
                highTemperatureSymptomId==lowTemperatureSymptomId || symptoms.Find(highTemperatureSymptomId)==null || symptoms.Find(lowTemperatureSymptomId)==null)
                throw new InvalidOperationException("Temperature symptom mappings are missing or invalid.");
            if(!npc.HasSickness)return 37;
            var sickness=medicalData.Find(npc.SicknessId);
            if(sickness==null)throw new InvalidOperationException("NPC references a missing sickness: "+npc.SicknessId);
            bool high=false,low=false;
            foreach(var id in sickness.SymptomIds)
            {
                if(symptoms.Find(id)==null)throw new InvalidOperationException("Sickness references a missing symptom: "+id);
                high |= id==highTemperatureSymptomId;
                low |= id==lowTemperatureSymptomId;
            }
            if(high && low)throw new InvalidOperationException("Sickness has conflicting high and low temperature symptoms.");
            // Symptoms determine temperature. The patient caches one sample for the whole consultation.
            float value=high?Mathf.Lerp(38.6f,42,Mathf.Clamp01(randomUnit)):
                low?Mathf.Lerp(33,34.9f,Mathf.Clamp01(randomUnit)):37;
            return Mathf.Round(value*10)/10;
        }
    }
}


