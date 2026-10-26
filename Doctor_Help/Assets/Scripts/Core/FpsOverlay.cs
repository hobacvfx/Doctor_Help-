using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [RequireComponent(typeof(Text))]
    public sealed class FpsOverlay : MonoBehaviour
    {
        private float elapsed;
        private int frames;
        private Text label;

        private void Awake() => label = GetComponent<Text>();

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < 0.5f) return;
            label.text = $"FPS: {frames / elapsed:0}  |  {elapsed * 1000f / frames:0.0} ms";
            elapsed = 0f;
            frames = 0;
        }

        private void OnApplicationPause(bool paused)
        {
            elapsed = 0f;
            frames = 0;
            label.text = "FPS: --";
        }
    }
}
