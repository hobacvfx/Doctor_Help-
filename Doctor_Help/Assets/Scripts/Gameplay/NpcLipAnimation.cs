using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class NpcLipAnimation : MonoBehaviour
    {
        [SerializeField] private Transform mouth;
        [SerializeField] private SpriteRenderer speakingLip;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(.1f)] private float framesPerSecond = 10;
        private readonly List<(GameObject part, bool active)> hiddenParts = new List<(GameObject, bool)>();
        private float elapsed;
        private bool IsTalking { get; set; }
        public bool IsConfigured => mouth != null && speakingLip != null && frames != null && frames.Length > 0 && !Array.Exists(frames, f => f == null);
        private void Awake() => SetTalking(false);
        public void SetTalking(bool talking)
        {
            if (talking == IsTalking && (talking || hiddenParts.Count == 0))
            { if (!talking && speakingLip != null) speakingLip.gameObject.SetActive(false); return; }
            if (talking)
            {
                if (!IsConfigured) throw new InvalidOperationException("NPC speaking requires a Mouth anchor, speaking renderer and ordered Lip frames.");
                hiddenParts.Clear();
                foreach (Transform child in mouth)
                {
                    hiddenParts.Add((child.gameObject, child.gameObject.activeSelf));
                    child.gameObject.SetActive(false);
                }
                elapsed = 0; speakingLip.sprite = frames[0]; speakingLip.gameObject.SetActive(true); IsTalking = true;
            }
            else
            {
                if (speakingLip != null) speakingLip.gameObject.SetActive(false);
                foreach (var part in hiddenParts) if (part.part != null) part.part.SetActive(part.active);
                hiddenParts.Clear(); IsTalking = false;
            }
        }
        private void Update()
        {
            if (!IsTalking) return;
            elapsed += Time.deltaTime;
            speakingLip.sprite = frames[(int)(elapsed * framesPerSecond) % frames.Length];
        }
        private void OnDisable() => SetTalking(false);
    }
}

