using System;
using UnityEngine;

namespace DoctorHelp
{
    // Remains on Desk, so deactivating the authored HeartCheck view cannot disable its controller.
    public sealed class HeartbeatExamination : MonoBehaviour
    {
        [SerializeField] private ExaminationToolSelection selection;
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GameObject heartCheck;
        [SerializeField] private ParticleSystem feedbackVfx;
        [SerializeField] private ParticleSystem soundVfx;
        [SerializeField] private AudioSource heartbeatAudio;
        [SerializeField] private AudioClip heartbeatClip;
        [SerializeField] private HeartbeatRhythm rhythm = new HeartbeatRhythm();
        [SerializeField, Min(0.001f)] private float noHeartbeatPreviewTime = 0.1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 4;
        [SerializeField, Range(0, 1)] private float maxVolume = 1;
        private Vector3 authoredSoundSize;
        private ParticleSystem.Particle[] particles;
        private bool sizesCaptured;
        private bool listening;
        private PatientExaminationProfile listenedPatient;
        private double nextBeatTime;
        private float previousGain;
        private string lastError;
        private float CurrentBpm { get; set; }
        private float CurrentGain { get; set; }

        private void Awake()
        {
            CaptureSizes();
            StopListening();
            if (heartCheck != null) heartCheck.SetActive(false);
        }
        private void CaptureSizes()
        {
            if (sizesCaptured || soundVfx == null) return;
            var main = soundVfx.main;
            authoredSoundSize = new Vector3(main.startSizeXMultiplier, main.startSizeYMultiplier, main.startSizeZMultiplier);
            particles = new ParticleSystem.Particle[main.maxParticles];
            sizesCaptured = true;
        }
        private void Update()
        {
            bool selected = selection != null && selection.IsSelected(ExaminationToolKind.Stethoscope);
            if (heartCheck != null && heartCheck.activeSelf != selected) heartCheck.SetActive(selected);
            if (!selected || !selection.IsUsingStethoscope)
            {
                if (listening) StopListening();
                return;
            }
            CaptureSizes();
            if (heartCheck == null || feedbackVfx == null || soundVfx == null || heartbeatAudio == null || heartbeatClip == null || targetCamera == null)
            {
                ReportError("Heartbeat examination has missing scene references.");
                StopListening();
                return;
            }
            var patient = patients != null ? patients.CurrentPatient : null;
            if (patient == null || patient.Heart == null)
            {
                ReportError("The current patient has no examination profile or Heart anchor.");
                StopListening();
                return;
            }
            if (listenedPatient != patient) listening = false;
            listenedPatient = patient;
            if (!listening)
            {
                try
                {
                    CurrentBpm = rhythm.Resolve(patient.Definition, patient.Profile != null ? patient.Profile.AllSickness : null);
                    lastError = null;
                }
                catch (InvalidOperationException error)
                {
                    // Invalid authored data stays visible in Console; never pretend it is a normal pulse.
                    ReportError(error.Message);
                    CurrentBpm = 0;
                }
            }
            Ray ray = targetCamera.ScreenPointToRay(selection.ScreenPosition);
            var plane = new Plane(Vector3.forward, patient.Heart.position);
            float distance = float.PositiveInfinity;
            if (plane.Raycast(ray, out float depth))
                distance = Vector2.Distance(ray.GetPoint(depth), patient.Heart.position);
            CurrentGain = CurrentBpm > 0 ? HeartbeatRhythm.DistanceGain(distance, maxDistance) : 0;
            ApplyGain(CurrentGain);
            double now = Time.timeAsDouble;
            if (!listening)
            {
                listening = true;
                nextBeatTime = now;
                if (CurrentBpm <= 0) ShowStaticFeedback();
            }
            if (CurrentBpm <= 0 || now < nextBeatTime) return;
            Restart(feedbackVfx);
            if (CurrentGain > 0)
            {
                Restart(soundVfx);
                heartbeatAudio.clip = heartbeatClip;
                heartbeatAudio.Play();
            }
            nextBeatTime = now + 60d / CurrentBpm;
        }
        private void ShowStaticFeedback()
        {
            // Materialize the authored heart once, then freeze its simulation. No fake pulses
            // are emitted for a patient without a heartbeat, even if the pointer moves.
            feedbackVfx.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            feedbackVfx.Simulate(Mathf.Max(0.001f, noHeartbeatPreviewTime), false, true, false);
            feedbackVfx.Pause(false);
        }
        private void ApplyGain(float gain)
        {
            var main = soundVfx.main;
            main.startSizeXMultiplier = authoredSoundSize.x * gain;
            if (main.startSize3D)
            {
                main.startSizeYMultiplier = authoredSoundSize.y * gain;
                main.startSizeZMultiplier = authoredSoundSize.z * gain;
            }
            // Scale already emitted particles too, without restarting the pulse when the pointer moves.
            if (gain > 0 && previousGain > 0 && !Mathf.Approximately(gain, previousGain))
            {
                if (particles == null || particles.Length < main.maxParticles)
                    particles = new ParticleSystem.Particle[main.maxParticles];
                int count = soundVfx.GetParticles(particles);
                float ratio = gain / previousGain;
                for (int i = 0; i < count; i++) particles[i].startSize3D *= ratio;
                soundVfx.SetParticles(particles, count);
            }
            heartbeatAudio.volume = maxVolume * gain;
            if (gain <= 0)
            {
                soundVfx.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                heartbeatAudio.Stop();
            }
            previousGain = gain;
        }
        private static void Restart(ParticleSystem effect)
        {
            effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(false);
        }
        private void StopListening()
        {
            listening = false;
            CurrentBpm = CurrentGain = previousGain = 0;
            if (feedbackVfx != null) feedbackVfx.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (soundVfx != null) soundVfx.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (heartbeatAudio != null) { heartbeatAudio.Stop(); heartbeatAudio.volume = 0; }
        }
        private void ReportError(string message)
        {
            if (lastError == message) return;
            lastError = message;
            Debug.LogError(message, this);
        }
        private void OnDisable()
        {
            StopListening();
            if (heartCheck != null) heartCheck.SetActive(false);
            if (!sizesCaptured || soundVfx == null) return;
            var main = soundVfx.main;
            main.startSizeXMultiplier = authoredSoundSize.x;
            if (main.startSize3D)
            {
                main.startSizeYMultiplier = authoredSoundSize.y;
                main.startSizeZMultiplier = authoredSoundSize.z;
            }
        }
    }
}





