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
        [SerializeField] private SpriteRenderer puffAnchor;
        [SerializeField] private DoctorBookClues clues;
        [SerializeField] private ParticleSystem visibilityPuff;
        [SerializeField] private TemperatureRules temperatureRules=new TemperatureRules();
        [SerializeField, Min(0.01f)] private float measurementSeconds=2;
        private PatientExaminationProfile measuredPatient;
        private bool measuring;
        private bool rejectedGesture;
        private bool clueReported;
        private double startedAt;
        private float TargetCelsius {get;set;}=ThermometerDisplay.MinimumCelsius;
        private void Awake()=>StopMeasurement();
        private void Update()
        {
            if(selection==null || !selection.IsUsingThermometer)
            { StopMeasurement(playPuff: true); return; }
            if(rejectedGesture)return;
            var patient=patients!=null?patients.CurrentPatient:null;
            if(measuring && patient!=measuredPatient)StopMeasurement(playPuff: true);
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
                    clueReported=false;
                    PlayPuff();
                }
                float elapsed=(float)(Time.timeAsDouble-startedAt);
                display.SetTemperature(Mathf.Lerp(ThermometerDisplay.MinimumCelsius,TargetCelsius,
                    Mathf.Clamp01(elapsed/Mathf.Max(0.01f,measurementSeconds))));
                if(!clueReported && elapsed>=measurementSeconds)
                {
                    clueReported=true;
                    if(clues!=null)clues.Measurement(patient,ClueMeasurement.Temperature,puffAnchor!=null?puffAnchor.bounds.center:display.transform.position);
                }
            }
            catch(InvalidOperationException error)
            {
                StopMeasurement(playPuff: true);rejectedGesture=true;
                Debug.LogError(error.Message,this);
            }
        }
        private void PlayPuff()
        {
            if (puffAnchor != null && display != null)
                ExaminationPuff.Play(visibilityPuff, display.gameObject, puffAnchor.bounds.center);
        }

        private void StopMeasurement(bool playPuff = false)
        {
            if (playPuff && measuring) PlayPuff();
            measuring=false;rejectedGesture=false;clueReported=false;measuredPatient=null;
            if(display!=null && display.gameObject.activeSelf)display.gameObject.SetActive(false);
        }
        private void OnDisable()=>StopMeasurement();
        private void OnApplicationPause(bool paused) { if(paused) StopMeasurement(); }
        private void OnApplicationFocus(bool focused) { if(!focused) StopMeasurement(); }
    }
}



