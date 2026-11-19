using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class PatientRockReaction : MonoBehaviour
    {
        [SerializeField] private GameObject hurtEyesPrefab;
        [SerializeField, Min(.1f)] private float hurtSeconds = 1f;
        [SerializeField] private Color flashTint = new Color(1f, .23f, .23f, 1f);
        [SerializeField] private string complaint = "Hey! what are you doing?";
        private bool completed;

        internal async Task PlayAsync(CancellationToken token)
        {
            if (completed) return;
            var patient = GetComponentInParent<PatientExaminationProfile>();
            var dialogue = patient.GetComponent<NpcDialogue>();
            dialogue?.CloseBox();
            var sprites = patient.GetComponentsInChildren<SpriteRenderer>();
            var colors = Array.ConvertAll(sprites, sprite => sprite.color);
            var eyeSlot = FindEyeSlot(patient.GetComponent<NpcAppearanceProfile>());
            var eyes = eyeSlot != null ? new GameObject[eyeSlot.childCount] : Array.Empty<GameObject>();
            var eyeStates = new bool[eyes.Length];
            for (int i = 0; i < eyes.Length; i++)
            {
                eyes[i] = eyeSlot.GetChild(i).gameObject;
                eyeStates[i] = eyes[i].activeSelf;
                eyes[i].SetActive(false);
            }
            var hurtEyes = eyeSlot != null ? Instantiate(hurtEyesPrefab, eyeSlot, false) : null;
            try
            {
                float elapsed = 0f;
                while (elapsed < hurtSeconds)
                {
                    bool flash = elapsed < .32f && Mathf.FloorToInt(elapsed / .08f) % 2 == 0;
                    for (int i = 0; i < sprites.Length; i++)
                        if (sprites[i] != null) sprites[i].color = flash ? colors[i] * flashTint : colors[i];
                    await Awaitable.NextFrameAsync(token);
                    elapsed += Time.deltaTime;
                }
            }
            finally
            {
                if (hurtEyes != null)
                {
                    hurtEyes.SetActive(false);
                    Destroy(hurtEyes);
                }
                for (int i = 0; i < eyes.Length; i++)
                    if (eyes[i] != null) eyes[i].SetActive(eyeStates[i]);
                for (int i = 0; i < sprites.Length; i++)
                    if (sprites[i] != null) sprites[i].color = colors[i];
            }

            if (dialogue != null)
            {
                dialogue.SpeakPatientComplaint(complaint);
                while (dialogue != null && dialogue.IsShowing)
                    await Awaitable.NextFrameAsync(token);
            }
            completed = true;
        }

        private static Transform FindEyeSlot(NpcAppearanceProfile appearance)
        {
            if (appearance != null)
                foreach (var slot in appearance.Slots)
                    if (slot != null && slot.id == "Eye") return slot.attachPoint;
            return null;
        }
    }
}

