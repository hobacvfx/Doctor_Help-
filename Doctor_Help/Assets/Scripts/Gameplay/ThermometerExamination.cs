using System;
using UnityEngine;
namespace DoctorHelp
{
    // Lives on Desk; the authored thermometer view is inactive between holds.
    public sealed class ThermometerExamination : MonoBehaviour
    {
        [SerializeField] private ExaminationToolSelection selection;
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private ThermometerDisplay display;
        [SerializeField] private TemperatureRules temperatureRules=new TemperatureRules();
        [SerializeField, Min(0.01f)] private float measurementSeconds=2;
        private PatientExaminationProfile measuredPatient;
        private bool measuring;
        private bool rejectedGesture;
        private double startedAt;
        private float TargetCelsius {get;set;}=ThermometerDisplay.MinimumCelsius;
        private void Awake()=>StopMeasurement();
        private void Update()
        {
            if(selection==null || !selection.IsUsingThermometer)
            { StopMeasurement(); return; }
            if(rejectedGesture)return;
            var patient=patients!=null?patients.CurrentPatient:null;
            if(measuring && patient!=measuredPatient)StopMeasurement();
            try
            {
                if(!measuring)
                {
                    if(display==null || !display.IsConfigured || patient==null || patient.Profile==null)
                        throw new InvalidOperationException("Thermometer scene references or current patient are missing.");
                    TargetCelsius=patient.GetConsultationTemperature(temperatureRules);
                    measuredPatient=patient;
                    startedAt=Time.timeAsDouble;
                    display.SetTemperature(ThermometerDisplay.MinimumCelsius);
                    display.gameObject.SetActive(true);
                    measuring=true;
                }
                float elapsed=(float)(Time.timeAsDouble-startedAt);
                display.SetTemperature(Mathf.Lerp(ThermometerDisplay.MinimumCelsius,TargetCelsius,
                    Mathf.Clamp01(elapsed/Mathf.Max(0.01f,measurementSeconds))));
            }
            catch(InvalidOperationException error)
            {
                StopMeasurement();rejectedGesture=true;
                Debug.LogError(error.Message,this);
            }
        }
        private void StopMeasurement()
        {
            measuring=false;rejectedGesture=false;measuredPatient=null;
            if(display!=null && display.gameObject.activeSelf)display.gameObject.SetActive(false);
        }
        private void OnDisable()=>StopMeasurement();
    }
}



