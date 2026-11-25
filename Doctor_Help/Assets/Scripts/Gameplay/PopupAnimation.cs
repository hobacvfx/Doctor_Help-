using System.Threading.Tasks;
using UnityEngine;

namespace DoctorHelp
{
    // Runs after popup layout so its scale is composed with Safe Area fitting.
    [DefaultExecutionOrder(10000), DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class PopupAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform[] targets = System.Array.Empty<RectTransform>();
        [SerializeField, Min(.01f)] private float duration = .35f;
        [SerializeField, Range(0f, .25f)] private float overshoot = .1f;
        private Vector3[] layoutScales;
        private CanvasGroup group;
        private TaskCompletionSource<bool> closing;
        private float elapsed, factor, from;
        private bool animating, hiding, applied, wasInteractable;

        public static void Show(GameObject popup)
        {
            if (popup == null) return;
            if (!popup.activeSelf) popup.SetActive(true);
            else if (popup.TryGetComponent<PopupAnimation>(out var animation) && animation.hiding)
                animation.Begin(false);
        }

        public static void Hide(GameObject popup) => _ = HideAsync(popup);

        public static Task HideAsync(GameObject popup)
        {
            if (popup == null) return Task.CompletedTask;
            if (!popup.activeInHierarchy)
            {
                popup.SetActive(false);
                return Task.CompletedTask;
            }
            if (!popup.TryGetComponent<PopupAnimation>(out var animation) || !animation.enabled)
            {
                popup.SetActive(false);
                return Task.CompletedTask;
            }
            // An initial scene hide can run before this component's Awake.
            // There is no visible opening pose to animate in that case.
            if (animation.group == null)
            {
                popup.SetActive(false);
                return Task.CompletedTask;
            }
            if (animation.closing != null) return animation.closing.Task;
            animation.closing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = animation.closing.Task;
            animation.Begin(true);
            return task;
        }

        private void EnsureInitialized()
        {
            if (group != null && layoutScales != null) return;
            group = GetComponent<CanvasGroup>();
            layoutScales = new Vector3[targets.Length];
            wasInteractable = group.interactable;
        }

        private void Awake() => EnsureInitialized();

        private void OnEnable()
        {
            EnsureInitialized();
            wasInteractable = group.interactable;
            factor = 0f;
            Begin(false);
            ApplyScale(); // Avoid a full-size flash before the first Update.
        }

        private void Begin(bool hide)
        {
            // Scene UI can hide us from its Awake before our own Awake runs.
            EnsureInitialized();
            if (!hide)
            {
                closing?.TrySetResult(false);
                closing = null;
            }
            hiding = hide;
            elapsed = 0f;
            from = factor;
            animating = true;
            group.interactable = false;
            // Keep the modal backdrop blocking the scene throughout the transition.
        }

        private void Update()
        {
            RestoreScale();
            if (!animating) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, duration));
            if (hiding)
            {
                // Anticipation: swell a little, then smoothly shrink away.
                factor = t < .25f
                    ? Mathf.Lerp(from, Mathf.Max(1f, from) + overshoot, Smooth(t / .25f))
                    : Mathf.Lerp(Mathf.Max(1f, from) + overshoot, 0f, Smooth((t - .25f) / .75f));
            }
            else
            {
                factor = t < .65f
                    ? Mathf.Lerp(from, 1f + overshoot, 1f - Mathf.Pow(1f - t / .65f, 3f))
                    : Mathf.Lerp(1f + overshoot, 1f, Smooth((t - .65f) / .35f));
            }
            if (t < 1f) return;
            animating = false;
            if (hiding) gameObject.SetActive(false);
            else group.interactable = wasInteractable;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private void LateUpdate()
        {
            if (animating) ApplyScale();
        }

        private void ApplyScale()
        {
            if (applied) return;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                layoutScales[i] = targets[i].localScale;
                targets[i].localScale = Vector3.Scale(layoutScales[i], new Vector3(factor, factor, 1f));
            }
            applied = true;
        }

        private void RestoreScale()
        {
            if (!applied) return;
            for (int i = 0; i < targets.Length; i++)
                if (targets[i] != null) targets[i].localScale = layoutScales[i];
            applied = false;
        }

        private void OnDisable()
        {
            EnsureInitialized();
            RestoreScale();
            animating = hiding = false;
            group.interactable = wasInteractable;
            var pending = closing;
            closing = null;
            pending?.TrySetResult(true);
        }
    }
}
