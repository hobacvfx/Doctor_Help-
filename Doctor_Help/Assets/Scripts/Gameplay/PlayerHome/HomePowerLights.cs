using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Player Home lamps follow the player's electricity: full power from LowElectricity up,
    // dimmed with random flickers below it, and off at zero.
    [DisallowMultipleComponent]
    public sealed class HomePowerLights : MonoBehaviour
    {
        internal const int LowElectricity = 50;
        private static readonly int PowerId = Shader.PropertyToID("_Power");

        [SerializeField] private Renderer[] lights = System.Array.Empty<Renderer>();
        [SerializeField, Min(0f)] private float lowPower = .5f;
        [Tooltip("Seconds between flickers while electricity is low.")]
        [SerializeField] private Vector2 flickerInterval = new Vector2(2f, 5f);
        [SerializeField, Min(.05f)] private float flickerSeconds = .35f;
        [Tooltip("Lowest Power reached during a flicker.")]
        [SerializeField, Min(0f)] private float flickerFloor = .05f;
        [Header("Warning (bottom of the screen)")]
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private GameObject warning;
        [SerializeField] private Text warningText;
        [SerializeField] private string lowMessage = "Your electric power is low";
        [SerializeField] private string outMessage = "Out of electric power";
        [Tooltip("Gentle pulse while the warning shows: scale amount, lowest alpha and cycles per second.")]
        [SerializeField, Range(0f, .2f)] private float pulseScale = .04f;
        [SerializeField, Range(0f, 1f)] private float pulseMinAlpha = .65f;
        [SerializeField, Min(.05f)] private float pulseSpeed = .8f;

        private PlayerProfileService profile;
        private MaterialPropertyBlock block;
        private float[] fullPower = System.Array.Empty<float>();
        private float nextFlicker, flickerStart = float.NegativeInfinity;
        private float appliedPower = float.NaN;
        private Graphic[] warningGraphics = System.Array.Empty<Graphic>();
        private float[] warningAlpha = System.Array.Empty<float>();
        private Vector3 warningScale = Vector3.one;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            fullPower = new float[lights.Length];
            for (int index = 0; index < lights.Length; index++)
            {
                var material = lights[index] != null ? lights[index].sharedMaterial : null;
                fullPower[index] = material != null && material.HasProperty(PowerId) ? material.GetFloat(PowerId) : 1f;
            }
            if (warning != null)
            {
                warningScale = warning.transform.localScale;
                warningGraphics = warning.GetComponentsInChildren<Graphic>(true);
                warningAlpha = new float[warningGraphics.Length];
                for (int index = 0; index < warningGraphics.Length; index++) warningAlpha[index] = warningGraphics[index].color.a;
            }
        }

        private void OnEnable()
        {
            profile = PlayerProfileService.Instance;
            ScheduleFlicker();
        }

        private void Update()
        {
            int electricity = profile != null && profile.IsLoaded ? profile.Electricity : LowElectricity;
            ShowWarning(electricity >= LowElectricity ? null : electricity <= 0 ? outMessage : lowMessage);
            if (electricity >= LowElectricity) { Apply(1f, full: true); return; }
            if (electricity <= 0) { Apply(0f, full: false); return; }

            if (Time.time >= nextFlicker)
            {
                flickerStart = Time.time;
                ScheduleFlicker();
            }
            Apply(lowPower * FlickerScale(Time.time - flickerStart), full: false);
        }

        // A short burst of uneven dips back to the dimmed level.
        private float FlickerScale(float elapsed)
        {
            if (elapsed < 0f || elapsed >= flickerSeconds) return 1f;
            float phase = elapsed / flickerSeconds;
            float dip = Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 3f)) * (1f - phase * .5f);
            float floor = lowPower > 0f ? Mathf.Clamp01(flickerFloor / lowPower) : 0f;
            return Mathf.Lerp(1f, floor, dip);
        }

        private void LateUpdate()
        {
            ScreenSafeArea.ApplyTo(safeArea);
            if (warning == null || !warning.activeSelf) return;
            // Soft breathing: 0..1 eased wave drives a slight scale-up and fade.
            float wave = .5f - .5f * Mathf.Cos(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
            warning.transform.localScale = warningScale * (1f + pulseScale * wave);
            float alpha = Mathf.Lerp(1f, pulseMinAlpha, wave);
            for (int index = 0; index < warningGraphics.Length; index++)
            {
                var graphic = warningGraphics[index];
                if (graphic == null) continue;
                var color = graphic.color;
                color.a = warningAlpha[index] * alpha;
                graphic.color = color;
            }
        }

        private void ShowWarning(string message)
        {
            if (warning == null) return;
            bool show = message != null;
            if (warning.activeSelf != show) warning.SetActive(show);
            if (show && warningText != null && warningText.text != message) warningText.text = message;
        }

        private void ScheduleFlicker() =>
            nextFlicker = Time.time + Random.Range(Mathf.Min(flickerInterval.x, flickerInterval.y),
                Mathf.Max(flickerInterval.x, flickerInterval.y));

        // full: each lamp's own material Power; otherwise an absolute Power for every lamp.
        private void Apply(float power, bool full)
        {
            float key = full ? -1f : power;
            if (key == appliedPower) return;
            appliedPower = key;
            for (int index = 0; index < lights.Length; index++)
            {
                var light = lights[index];
                if (light == null) continue;
                light.GetPropertyBlock(block);
                block.SetFloat(PowerId, full ? fullPower[index] : power);
                light.SetPropertyBlock(block);
            }
        }
    }
}
