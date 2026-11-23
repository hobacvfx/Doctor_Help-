using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Reward presentation shared by every place that gives gold, clean water or electricity:
    // a VFX_Puff where the reward is received, then a stream of currency icons flying into the Headbar.
    // The profile is credited first; the bar holds that amount back and counts up as each icon lands.
    public sealed partial class Headbar
    {
        private const int FlightSortingOrder = 450; // Above gameplay UI and popups, below the FPS readout (500).
        private const int PuffSortingOrder = 32000;

        [Header("Reward flight")]
        [Tooltip("Nested canvas the flying icons are drawn in; its sorting is raised at runtime.")]
        [SerializeField] private RectTransform flightLayer;
        [SerializeField] private ParticleSystem rewardPuff;

        private readonly int[] held = new int[3];
        private readonly float[] punch = new float[3];
        private readonly Image[] icons = new Image[3];
        private readonly Vector3[] iconScales = new Vector3[3];
        private readonly Stack<Image> idleCoins = new Stack<Image>();
        private readonly List<Image> flyingCoins = new List<Image>();
        private readonly Stack<RawImage> idlePuffs = new Stack<RawImage>();
        private readonly List<RawImage> playingPuffs = new List<RawImage>();
        private Canvas flightCanvas;

        // Below 50: 10 icons, 50-149: 20 icons, 150 and above: 30 icons.
        internal static int RewardIconCount(int amount) => amount < 50 ? 10 : amount < 150 ? 20 : 30;

        // Keeps an amount the profile already holds out of the shown value until its icons land.
        internal void Hold(CurrencyKind kind, int amount)
        {
            if (amount <= 0) return;
            held[(int)kind] = (int)System.Math.Min((long)held[(int)kind] + amount, int.MaxValue);
            Refresh();
        }

        // Flies icons from a screen point into the bar. Call after the profile is credited.
        internal void FlyReward(CurrencyKind kind, int amount, Vector2 screenFrom, bool alreadyHeld = false)
        {
            if (amount <= 0) return;
            var icon = Icon(kind);
            if (!isActiveAndEnabled || flightLayer == null || icon == null)
            {
                if (alreadyHeld) Release(kind, amount);
                return;
            }
            if (!alreadyHeld) Hold(kind, amount);
            StartCoroutine(SpawnIcons(kind, amount, screenFrom, icon, null));
        }

        // Same icon flight for a bar whose amount text is driven by its owner (the end-of-day report, whose
        // Headbar component is disabled so it does not show the profile): only the GameObject must be active.
        // The shown amounts are left alone and landed(share) is called as each icon arrives.
        // Returns false when nothing will fly, so the caller should apply the amount itself.
        internal bool FlyIcons(CurrencyKind kind, int amount, Vector2 screenFrom, Action<int> landed)
        {
            var icon = Icon(kind);
            if (amount <= 0 || !gameObject.activeInHierarchy || flightLayer == null || icon == null) return false;
            StartCoroutine(SpawnIcons(kind, amount, screenFrom, icon, landed));
            return true;
        }

        // VFX_Puff drawn as UI in the flight layer, for rewards received on top of a full-screen popup
        // where a world puff would be hidden. Plays the puff's flipbook once.
        internal void PlayUiPuff(Vector2 screen)
        {
            if (rewardPuff == null || flightLayer == null || !gameObject.activeInHierarchy) return;
            var renderer = rewardPuff.GetComponent<ParticleSystemRenderer>();
            var material = renderer != null ? renderer.sharedMaterial : null;
            if (material == null) return;
            var texture = material.mainTexture;
            if (texture == null)
                foreach (var property in material.GetTexturePropertyNames())
                    if ((texture = material.GetTexture(property)) != null) break;
            if (texture == null) return;
            // The puff material blends additively over a black background; UI blends by alpha, so use a copy
            // whose alpha comes from brightness (black becomes transparent) instead of drawing a black square.
            if (material.HasProperty("_DstBlend") && Mathf.Approximately(material.GetFloat("_DstBlend"), 1f))
                texture = AlphaFromBrightness(texture);
            StartCoroutine(PlayUiPuff(screen, texture));
        }

        private static readonly Dictionary<Texture, Texture2D> brightnessAlpha = new Dictionary<Texture, Texture2D>();

        private static Texture AlphaFromBrightness(Texture source)
        {
            if (brightnessAlpha.TryGetValue(source, out var cached) && cached != null) return cached;
            int width = source.width, height = source.height;
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var copy = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = source.name + " (UI alpha)" };
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            var pixels = copy.GetPixels32();
            for (int index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                byte alpha = (byte)Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                pixels[index] = alpha == 0 ? new Color32(255, 255, 255, 0) : new Color32(
                    (byte)(pixel.r * 255 / alpha), (byte)(pixel.g * 255 / alpha), (byte)(pixel.b * 255 / alpha), alpha);
            }
            copy.SetPixels32(pixels);
            copy.wrapMode = TextureWrapMode.Clamp;
            copy.filterMode = source.filterMode;
            copy.Apply(false, true);
            brightnessAlpha[source] = copy;
            return copy;
        }

        // VFX_Puff at a screen point (UI rewards), drawn in front of the given camera's world.
        internal void PlayRewardPuff(Vector2 screen, Camera camera)
        {
            if (rewardPuff == null) return;
            if (camera == null) camera = Camera.main;
            if (camera == null) return;

            var view = camera.transform;
            float depth = Mathf.Abs(view.forward.z) > .01f ? -view.position.z / view.forward.z - .5f : 0f;
            if (!(depth > camera.nearClipPlane + .1f)) depth = camera.nearClipPlane + 1f;
            var position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            var puff = Instantiate(rewardPuff, position, rewardPuff.transform.rotation);

            int layer = puff.gameObject.layer;
            if ((camera.cullingMask & (1 << layer)) == 0)
                for (int bit = 0; bit < 32; bit++)
                    if ((camera.cullingMask & (1 << bit)) != 0) { layer = bit; break; }
            var layers = SortingLayer.layers;
            int topLayer = layers.Length > 0 ? layers[layers.Length - 1].id : 0;
            foreach (var part in puff.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
            foreach (var renderer in puff.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                renderer.sortingLayerID = topLayer;
                renderer.sortingOrder = PuffSortingOrder;
            }
            var settings = puff.main;
            settings.stopAction = ParticleSystemStopAction.Destroy;
            puff.Play(true);
        }

        private void PrepareFlightLayer()
        {
            if (flightLayer == null) return;
            flightCanvas = flightLayer.GetComponent<Canvas>();
            if (flightCanvas == null) return;
            flightCanvas.overrideSorting = true;
            flightCanvas.sortingOrder = FlightSortingOrder;
        }

        private Image Icon(CurrencyKind kind)
        {
            int index = (int)kind;
            if (icons[index] != null) return icons[index];
            var amount = Amount(kind);
            if (amount == null || amount.transform.parent == null) return null;
            // Each slot is an icon Image next to its amount Text.
            foreach (var image in amount.transform.parent.GetComponentsInChildren<Image>(true))
            {
                icons[index] = image;
                iconScales[index] = image.rectTransform.localScale;
                break;
            }
            return icons[index];
        }

        private string Shown(CurrencyKind kind, int value) => Mathf.Max(0, value - held[(int)kind]).ToString();

        private void Release(CurrencyKind kind, int amount)
        {
            int index = (int)kind;
            held[index] = Mathf.Max(0, held[index] - amount);
            Refresh();
        }

        private IEnumerator PlayUiPuff(Vector2 screen, Texture texture)
        {
            var root = flightCanvas != null ? flightCanvas.rootCanvas : GetComponentInParent<Canvas>();
            var eye = root == null || root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(flightLayer, screen, eye, out var local);

            RawImage puff;
            if (idlePuffs.Count > 0) puff = idlePuffs.Pop();
            else
            {
                var go = new GameObject("Reward Puff", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                go.layer = flightLayer.gameObject.layer;
                puff = go.GetComponent<RawImage>();
                puff.raycastTarget = false;
                puff.rectTransform.SetParent(flightLayer, false);
                puff.rectTransform.anchorMin = puff.rectTransform.anchorMax = puff.rectTransform.pivot = new Vector2(.5f, .5f);
            }
            playingPuffs.Add(puff);
            puff.texture = texture;
            var settings = rewardPuff.main;
            puff.color = settings.startColor.color;
            var rect = puff.rectTransform;
            rect.sizeDelta = Vector2.one * 230f * ReferenceUnit();
            rect.localPosition = local;
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            rect.SetAsLastSibling();
            puff.gameObject.SetActive(true);

            var sheet = rewardPuff.textureSheetAnimation;
            int columns = sheet.enabled ? Mathf.Max(1, sheet.numTilesX) : 1;
            int rows = sheet.enabled ? Mathf.Max(1, sheet.numTilesY) : 1;
            int frames = columns * rows;
            float lifetime = Mathf.Max(.1f, settings.startLifetime.constant);
            for (float elapsed = 0f; elapsed < lifetime; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / lifetime;
                int frame = Mathf.Min(frames - 1, (int)(t * frames));
                int column = frame % columns, row = frame / columns;
                puff.uvRect = new Rect((float)column / columns, 1f - (row + 1f) / rows, 1f / columns, 1f / rows);
                rect.localScale = Vector3.one * Mathf.Lerp(.85f, 1.15f, t);
                yield return null;
            }
            ReturnPuff(puff);
        }

        private void ReturnPuff(RawImage puff)
        {
            playingPuffs.Remove(puff);
            puff.gameObject.SetActive(false);
            idlePuffs.Push(puff);
        }

        private IEnumerator SpawnIcons(CurrencyKind kind, int amount, Vector2 screenFrom, Image icon, Action<int> landed)
        {
            int count = RewardIconCount(amount);
            float window = Mathf.Lerp(1f, 2f, (count - 10) / 20f); // Spread over 1-2 seconds.
            var root = flightCanvas != null ? flightCanvas.rootCanvas : GetComponentInParent<Canvas>();
            var eye = root == null || root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(flightLayer, screenFrom, eye, out var origin);

            int paid = 0;
            for (int index = 0; index < count; index++)
            {
                int share = (int)((long)amount * (index + 1) / count) - paid;
                paid += share;
                StartCoroutine(FlyIcon(kind, share, origin, icon, landed));
                float wait = window / count * UnityEngine.Random.Range(.55f, 1.45f);
                for (float waited = 0f; waited < wait; waited += Time.unscaledDeltaTime) yield return null;
            }
        }

        private IEnumerator FlyIcon(CurrencyKind kind, int share, Vector2 origin, Image icon, Action<int> landed)
        {
            var coin = TakeCoin(icon);
            var rect = coin.rectTransform;
            float unit = ReferenceUnit();

            // Burst out in a random upward fan, then curve into the slot from below at a random speed.
            Vector2 start = origin + UnityEngine.Random.insideUnitCircle * 40f * unit; // Local to the layer's pivot.
            float angle = UnityEngine.Random.Range(15f, 165f) * Mathf.Deg2Rad;
            Vector2 burst = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(260f, 560f) * unit;
            Vector2 sway = new Vector2(UnityEngine.Random.Range(-1f, 1f) * UnityEngine.Random.Range(120f, 380f), -UnityEngine.Random.Range(80f, 320f)) * unit;
            float duration = UnityEngine.Random.Range(.7f, 1.1f);
            float spin = UnityEngine.Random.Range(-25f, 25f);
            float phase = UnityEngine.Random.value * Mathf.PI * 2f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                Vector2 end = Target(kind);
                Vector2 a = start + burst, b = end + sway;
                float u = 1f - t;
                rect.localPosition = u * u * u * start + 3f * u * u * t * a + 3f * u * t * t * b + t * t * t * end;
                rect.localScale = Vector3.one * (t < .12f ? Mathf.Lerp(.35f, 1.12f, t / .12f) :
                    t < .22f ? Mathf.Lerp(1.12f, 1f, (t - .12f) / .1f) :
                    t > .85f ? Mathf.Lerp(1f, .7f, (t - .85f) / .15f) : 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, spin * Mathf.Sin(phase + t * 9f));
                yield return null;
            }

            ReturnCoin(coin);
            punch[(int)kind] = 1f;
            if (landed != null) landed(share);
            else Release(kind, share);
        }

        private void Update()
        {
            for (int index = 0; index < punch.Length; index++)
            {
                if (punch[index] <= 0f || icons[index] == null) continue;
                punch[index] = Mathf.Max(0f, punch[index] - Time.unscaledDeltaTime / .18f);
                icons[index].rectTransform.localScale = iconScales[index] * (1f + .22f * punch[index]);
            }
        }

        private Vector2 Target(CurrencyKind kind)
        {
            var icon = icons[(int)kind].rectTransform;
            return flightLayer.InverseTransformPoint(icon.TransformPoint(icon.rect.center));
        }

        private float ReferenceUnit()
        {
            var root = flightCanvas != null ? flightCanvas.rootCanvas : GetComponentInParent<Canvas>();
            if (root == null) return 1f;
            var rootRect = (RectTransform)root.transform;
            float layerScale = Mathf.Max(.0001f, flightLayer.lossyScale.y);
            return rootRect.rect.height * rootRect.lossyScale.y / layerScale / 1920f;
        }

        private Image TakeCoin(Image icon)
        {
            Image coin;
            if (idleCoins.Count > 0) coin = idleCoins.Pop();
            else
            {
                var go = new GameObject("Reward Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = flightLayer.gameObject.layer;
                coin = go.GetComponent<Image>();
                coin.raycastTarget = false;
                coin.preserveAspect = true;
                var rect = coin.rectTransform;
                rect.SetParent(flightLayer, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            }
            coin.sprite = icon.sprite;
            coin.color = icon.color;
            coin.material = icon.material == icon.defaultMaterial ? null : icon.material;
            var iconRect = icon.rectTransform;
            float ratio = iconRect.lossyScale.x / Mathf.Max(.0001f, flightLayer.lossyScale.x);
            coin.rectTransform.sizeDelta = iconRect.rect.size * ratio;
            coin.rectTransform.SetAsLastSibling();
            coin.gameObject.SetActive(true);
            flyingCoins.Add(coin);
            return coin;
        }

        private void ReturnCoin(Image coin)
        {
            flyingCoins.Remove(coin);
            coin.gameObject.SetActive(false);
            idleCoins.Push(coin);
        }

        // For owners of a disabled bar (OnDisable does not run for it): drop icons and puffs still in the air.
        internal void StopRewardFlights() => StopFlights();

        private void StopFlights()
        {
            StopAllCoroutines();
            for (int index = flyingCoins.Count - 1; index >= 0; index--)
                if (flyingCoins[index] != null) ReturnCoin(flyingCoins[index]);
            flyingCoins.Clear();
            for (int index = playingPuffs.Count - 1; index >= 0; index--)
                if (playingPuffs[index] != null) ReturnPuff(playingPuffs[index]);
            playingPuffs.Clear();
            for (int index = 0; index < held.Length; index++)
            {
                held[index] = 0;
                punch[index] = 0f;
                if (icons[index] != null) icons[index].rectTransform.localScale = iconScales[index];
            }
        }
    }
}
