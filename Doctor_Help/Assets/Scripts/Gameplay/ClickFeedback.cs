using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Projects the actual animated particle mesh onto a scene-owned overlay Canvas.
    // This keeps the click effect above both world objects and screen-space popups.
    [DefaultExecutionOrder(-250), DisallowMultipleComponent]
    public sealed class ClickFeedback : MaskableGraphic
    {
        [SerializeField] private ParticleSystem effectPrefab;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Transform instancesRoot;
        [SerializeField, Range(1, 64)] private int maximumEffects = 32;
        private sealed class Effect
        {
            public ParticleSystem particles;
            public ParticleSystemRenderer renderer;
            public Mesh mesh;
            public Vector2 viewportPosition;
        }
        private readonly List<Effect> effects = new List<Effect>();
        private readonly HashSet<int> previousTouches = new HashSet<int>();
        private readonly HashSet<int> currentTouches = new HashSet<int>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector4> uvs = new List<Vector4>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();
        private bool mouseWasHeld, paused, unfocused;
        public bool IsConfigured => effectPrefab != null && !effectPrefab.main.loop &&
            effectPrefab.GetComponent<ParticleSystemRenderer>() != null && sceneCamera != null && instancesRoot != null;
        private Material SourceMaterial => effectPrefab != null ? effectPrefab.GetComponent<ParticleSystemRenderer>()?.sharedMaterial : null;
        public override Texture mainTexture
        {
            get
            {
                var source = SourceMaterial;
                if (source == null) return Texture2D.whiteTexture;
                return (source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture) ?? Texture2D.whiteTexture;
            }
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            ResetInput();
        }
        private void ResetInput()
        {
            mouseWasHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
            previousTouches.Clear(); currentTouches.Clear();
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
                foreach (var touch in touchscreen.touches)
                    if (touch.press.isPressed) previousTouches.Add(touch.touchId.ReadValue());
        }
        private void Update()
        {
            if (!Application.isPlaying) return;
            if (paused || unfocused) { ResetInput(); return; }
            bool touchGesture = previousTouches.Count > 0;
            currentTouches.Clear();
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.isPressed) continue;
                    int id = touch.touchId.ReadValue();
                    currentTouches.Add(id); touchGesture = true;
                    if (!previousTouches.Contains(id)) Spawn(touch.position.ReadValue());
                }
            previousTouches.Clear(); previousTouches.UnionWith(currentTouches);
            var mouse = Mouse.current;
            bool held = mouse != null && mouse.leftButton.isPressed;
            if (!touchGesture && held && !mouseWasHeld) Spawn(mouse.position.ReadValue());
            mouseWasHeld = held;
        }
        private void Spawn(Vector2 point)
        {
            if (point.x < 0 || point.y < 0 || point.x >= Screen.width || point.y >= Screen.height) return;
            if (!IsConfigured)
            { Debug.LogError("Click feedback requires a one-shot particle prefab, camera and scene instance root.", this); return; }
            if (effects.Count >= maximumEffects) RemoveAt(0);
            var particles = Instantiate(effectPrefab, instancesRoot, false);
            particles.transform.position = Vector3.zero;
            var main = particles.main;
            main.useUnscaledTime = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.enabled = false; // Its mesh is drawn by this non-raycasting overlay Graphic.
            effects.Add(new Effect { particles = particles, renderer = renderer,
                mesh = new Mesh { name = "Click particle mesh" },
                viewportPosition = new Vector2(point.x / Screen.width, point.y / Screen.height) });
            particles.Play(false);
            SetVerticesDirty();
        }
        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            bool changed = effects.Count > 0;
            for (int i = effects.Count - 1; i >= 0; i--)
                if (effects[i].particles == null || !effects[i].particles.IsAlive(true)) RemoveAt(i);
            if (changed) SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (sceneCamera == null || Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 origin = sceneCamera.WorldToScreenPoint(Vector3.zero);
            var source = SourceMaterial;
            Color tint = color * (source != null && source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white);
            foreach (var effect in effects)
            {
                if (effect.particles == null || effect.renderer == null) continue;
                effect.renderer.BakeMesh(effect.mesh, sceneCamera,
                    ParticleSystemBakeMeshOptions.BakePosition | ParticleSystemBakeMeshOptions.BakeRotationAndScale);
                effect.mesh.GetVertices(vertices); effect.mesh.GetUVs(0, uvs);
                effect.mesh.GetColors(colors); effect.mesh.GetTriangles(triangles, 0);
                int first = helper.currentVertCount;
                if (first + vertices.Count > 65000) break;
                var anchor = new Vector2(effect.viewportPosition.x * Screen.width, effect.viewportPosition.y * Screen.height);
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector2 screen = (Vector2)sceneCamera.WorldToScreenPoint(vertices[i]) - origin + anchor;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen,
                        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
                    var vertex = UIVertex.simpleVert;
                    vertex.position = local;
                    vertex.uv0 = i < uvs.Count ? uvs[i] : Vector4.zero;
                    vertex.color = (i < colors.Count ? (Color)colors[i] : Color.white) * tint;
                    helper.AddVert(vertex);
                }
                for (int i = 0; i + 2 < triangles.Count; i += 3)
                    helper.AddTriangle(first + triangles[i], first + triangles[i + 1], first + triangles[i + 2]);
            }
        }
        private void RemoveAt(int index)
        {
            var effect = effects[index];
            if (effect.particles != null) Destroy(effect.particles.gameObject);
            if (effect.mesh != null) Destroy(effect.mesh);
            effects.RemoveAt(index);
        }
        protected override void OnDisable()
        {
            for (int i = effects.Count - 1; i >= 0; i--) RemoveAt(i);
            ResetInput(); base.OnDisable();
        }
        private void OnApplicationFocus(bool focused) { unfocused = !focused; ResetInput(); }
        private void OnApplicationPause(bool value) { paused = value; ResetInput(); }
    }
}
