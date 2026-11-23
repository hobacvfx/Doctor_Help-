using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // Player_Home presentation of the basement defense. It registers the scene layout with
    // BasementDefenseService and then only mirrors the simulation: rats on the pipe (slowed ones tinted), plants
    // on the shelves (clip, time, petals), needles/petals in flight, electric streams, hit impacts (needle puff,
    // petal burst), deaths (needle/petal: the rat falls to the floor and sinks out of sight; electric: it burns
    // to ash), the generator and its wiring, and the world buttons. Nothing here decides gameplay or applies
    // damage, so rebuilding the view (returning to Home, re-enabling it) never repeats a shot or an effect.
    // Shelf buttons open the protection popup (BasementProtectionFlow) for their own shelf.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)] // Before Spine renders, so plant poses set here show in the same frame.
    public sealed class BasementDefenseView : MonoBehaviour
    {
        private const int PathSamples = 1024;

        [SerializeField] private BasementPipePath path;
        [Tooltip("Range is measured in metres of this transform: 1 local unit = 1 m (Home: world units ÷ its scale).")]
        [SerializeField] private Transform rangeSpace;
        [SerializeField] private BasementPlantSlot[] slots = new BasementPlantSlot[0];
        [SerializeField] private BasementProtectionFlow protection;
        [SerializeField] private BasementRatView ratPrefab;
        [Tooltip("Parent of spawned rat visuals.")]
        [SerializeField] private Transform ratLayer;
        [SerializeField] private GeneratorAnimationController generator;
        [SerializeField] private GeneratorWiringAnimationController wiring;
        [Tooltip("Shelf button caption while the shelf is empty.")]
        [SerializeField] private string addProtectionLabel = "Add protection";
        [Tooltip("Shelf button caption while a plant stands on the shelf.")]
        [SerializeField] private string protectionLabel = "Protection";
        [Header("Shots (presentation of simulated projectiles)")]
        [Tooltip("Upward-facing needle sprite, bottom-centre pivot.")]
        [SerializeField] private Sprite needleSprite;
        [SerializeField, Min(.01f)] private float needleScale = .8f;
        [Tooltip("Upward-facing petal sprite, bottom-centre pivot.")]
        [SerializeField] private Sprite petalSprite;
        [SerializeField, Min(.01f)] private float petalScale = .45f;
        [SerializeField] private int shotSortingOrder = 17;
        [Tooltip("Electric stream material (additive/sprite, uses vertex colour).")]
        [SerializeField] private Material arcMaterial;
        [SerializeField] private Color arcColor = new Color(.55f, 1f, 1f, 1f);
        [SerializeField, Min(.01f)] private float arcWidth = .16f;
        [Tooltip("Soft halo drawn under the stream (width ×, alpha ×). 0 width: no halo.")]
        [SerializeField, Min(0f)] private float arcGlowWidth = 3.2f;
        [SerializeField, Range(0f, 1f)] private float arcGlowAlpha = .3f;
        [SerializeField, Range(2, 32)] private int arcSegments = 12;
        [Tooltip("Sideways jitter of the stream, world units.")]
        [SerializeField, Min(0f)] private float arcJitter = .28f;
        [Header("Impacts and deaths (presentation of simulated hits)")]
        [Tooltip("Played where a needle hits (VFX_Puff).")]
        [SerializeField] private GameObject needleImpact;
        [Tooltip("World scale of the needle impact (the particle system scales with its transform).")]
        [SerializeField, Min(.01f)] private float needleImpactScale = .8f;
        [SerializeField, Min(.1f)] private float needleImpactSeconds = 1.2f;
        [Tooltip("Frames played where a petal hits, in order.")]
        [SerializeField] private Sprite[] petalImpactFrames = new Sprite[0];
        [SerializeField, Min(1f)] private float petalImpactFps = 24f;
        [SerializeField, Min(.01f)] private float petalImpactScale = .7f;
        [Tooltip("Frames replacing a rat killed by the electric stream (the art faces up like the rat; first frame = the rat).")]
        [SerializeField] private Sprite[] electrocutedFrames = new Sprite[0];
        [SerializeField, Min(1f)] private float electrocutedFps = 14f;
        [SerializeField] private int impactSortingOrder = 18;
        [Tooltip("Floor line: rats killed by needles or petals fall to it and sink out of sight below it.")]
        [SerializeField] private Transform ratGround;
        [Tooltip("Fall acceleration, metres per second².")]
        [SerializeField, Min(.1f)] private float fallGravityMetres = 12f;
        [Tooltip("Upward kick when hit, metres per second.")]
        [SerializeField, Min(0f)] private float fallKickMetres = 1.2f;
        [Tooltip("Tumble while falling, degrees per second (random direction).")]
        [SerializeField, Min(0f)] private float fallSpinDegrees = 220f;
        [Tooltip("Speed of sinking into the floor after landing, metres per second.")]
        [SerializeField, Min(.05f)] private float sinkMetres = .9f;
        [Tooltip("Scene gizmo only: draws this Range (m) around every shelf.")]
        [SerializeField, Min(0f)] private float gizmoRangeMetres = 5f;
        [Header("Repair")]
        [SerializeField] private DeskActionButton repairButton;
        [Tooltip("Shown near the Repair button when the player cannot afford it.")]
        [SerializeField] private TextMesh repairMessage;
        [SerializeField] private string notEnoughGoldMessage = "Not enough gold";
        [SerializeField] private string unavailableMessage = "Repair after the clinic day";
        [SerializeField, Min(.5f)] private float messageSeconds = 3f;

        private readonly Dictionary<int, BasementRatView> rats = new Dictionary<int, BasementRatView>();
        private readonly List<int> staleRats = new List<int>();
        private readonly Dictionary<int, Vector3> catchStarts = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, SpriteRenderer> shots = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, LineRenderer> arcs = new Dictionary<int, LineRenderer>();
        private readonly List<int> staleShots = new List<int>();
        private readonly HashSet<int> liveShots = new HashSet<int>();
        private BasementDefenseService service;
        private float messageUntil;
        private float depth;
        // Hits already shown; starts at the simulation's current hit when the view appears (no replay).
        private long hitCursor = -1;
        private BasementDefenseSimulation hitSource;
        private readonly List<BasementHitEvent> hits = new List<BasementHitEvent>();
        private readonly List<Flipbook> flipbooks = new List<Flipbook>();
        private readonly Stack<SpriteRenderer> flipbookPool = new Stack<SpriteRenderer>();
        private readonly List<(GameObject effect, float until)> puffs = new List<(GameObject, float)>();
        private readonly List<FallingRat> falling = new List<FallingRat>();
        private SpriteMask groundMask;
        private Sprite maskSprite;

        private sealed class Flipbook
        {
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public float Fps, Age;
        }

        private sealed class FallingRat
        {
            public BasementRatView View;
            public SpriteRenderer Sprite;
            public Vector3 Velocity;
            public float Spin;
            public bool Landed;
        }

        private void OnEnable()
        {
            service = BasementDefenseService.Instance;
            foreach (var slot in slots)
                if (slot != null && slot.ProtectionButton != null) slot.ProtectionButton.Clicked += Requester(slot);
            if (repairButton != null) repairButton.Clicked += Repair;
            if (repairMessage != null) repairMessage.gameObject.SetActive(false);
            if (service == null) { Debug.LogError("BasementDefenseService is missing; the basement is idle.", this); return; }
            if (path == null || !path.IsValid) { Debug.LogError("Basement pipe path needs at least two waypoints.", this); return; }
            service.RegisterLayout(BuildLayout());
            Refresh(); // Show the current simulation state immediately, without replaying past events.
        }

        private float WorldPerMetre => rangeSpace != null ? Mathf.Abs(rangeSpace.lossyScale.x) : 1f;

        private BasementLayout BuildLayout()
        {
            var layout = new BasementLayout();
            var samples = new Vector2[PathSamples + 1];
            for (int index = 0; index <= PathSamples; index++) samples[index] = path.Evaluate(index / (float)PathSamples);
            layout.SetPath(samples, WorldPerMetre);
            depth = path.Evaluate(0f).z;
            foreach (var slot in slots)
                if (slot != null)
                    layout.Add(slot.SlotId, path.WindowsWithin(slot.MouthCenter, slot.MouthRadius), slot.PlantAnchor.position);
            // Plants shown before this registration measured their muzzles into the previous layout.
            foreach (var slot in slots) if (slot != null) slot.PlantInstanceId = int.MinValue;
            return layout;
        }

        private readonly Dictionary<BasementPlantSlot, System.Action> requesters = new Dictionary<BasementPlantSlot, System.Action>();

        private System.Action Requester(BasementPlantSlot slot)
        {
            if (!requesters.TryGetValue(slot, out var action))
                requesters[slot] = action = () => { if (protection != null) protection.RequestProtection(slot.SlotId); };
            return action;
        }

        private void OnDisable()
        {
            foreach (var slot in slots)
                if (slot != null && slot.ProtectionButton != null) slot.ProtectionButton.Clicked -= Requester(slot);
            if (repairButton != null) repairButton.Clicked -= Repair;
            ClearEffects();
        }

        private void LateUpdate()
        {
            // After a Play Mode code reload the service may enable after this view: register once it is back.
            if (service == null || !service.IsRunning)
            {
                service = BasementDefenseService.Instance;
                if (service != null && service.Simulation != null && path != null && path.IsValid)
                    service.RegisterLayout(BuildLayout());
            }
            Refresh();
            AnimateEffects(Time.unscaledDeltaTime);
        }

        private void Refresh()
        {
            if (service == null || service.Simulation == null || !service.IsRunning) return;
            var simulation = service.Simulation;
            var data = simulation.Data;
            RefreshPlants(data);
            ShowHits(simulation); // Before RefreshRats: a killed rat's visual is handed over to its death effect.
            RefreshRats(data);
            RefreshShots(data, simulation);
            RefreshArcs(data, simulation);
            if (generator != null) generator.SetOperating(simulation.GeneratorWorking);
            if (wiring != null) wiring.ShowSimulatedState(data.wire, data.wireSeconds);
            if (repairButton != null) repairButton.gameObject.SetActive(simulation.GeneratorBroken);
            if (repairMessage != null && repairMessage.gameObject.activeSelf &&
                (Time.unscaledTime >= messageUntil || !simulation.GeneratorBroken))
                repairMessage.gameObject.SetActive(false);
        }

        private void RefreshPlants(BasementDefenseData data)
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                var equip = data.Equip(slot.SlotId);
                var legacy = equip == null ? data.Legacy(slot.SlotId) : null; // Old save, ownership pending.
                var kind = equip != null ? service.Owned(equip.instanceId).Kind
                    : legacy != null ? service.Catalog.Find(legacy.plantId) : null;
                int instanceId = equip != null ? equip.instanceId : legacy != null ? -1 : 0;
                SetLabel(slot, kind != null ? protectionLabel : addProtectionLabel);

                if (slot.PlantObject != null && (kind == null || slot.PlantInstanceId != instanceId))
                {
                    // Swapped, sold out of existence (new profile) or no longer usable: rebuild from the saved state.
                    Destroy(slot.PlantObject);
                    slot.PlantObject = null;
                    slot.Plant = null;
                    slot.Ranged = null;
                }
                slot.PlantInstanceId = kind != null ? instanceId : 0;
                if (kind == null || kind.Prefab == null) continue;
                if (slot.PlantObject == null)
                {
                    slot.PlantObject = Instantiate(kind.Prefab, slot.PlantAnchor, false);
                    slot.PlantObject.name = kind.Prefab.name;
                    slot.Plant = slot.PlantObject.GetComponent<RatEater>();
                    slot.Ranged = slot.PlantObject.GetComponent<DefensePlantAnimation>();
                    if (slot.Ranged != null)
                    {
                        // Shots start where this kind's muzzle bone sits in its rest pose on this shelf.
                        slot.Ranged.ShowStrike(false, 0f, 1f);
                        slot.Ranged.RefreshPose();
                        if (slot.Ranged.TryGetMuzzlePosition(out var muzzle)) service.SetMuzzle(slot.SlotId, kind.PlantId, muzzle);
                    }
                }
                var runtime = equip != null ? data.Runtime(equip.instanceId) : null;
                var state = runtime != null ? runtime.state : legacy != null ? legacy.state : BasementPlantState.Ready;
                float seconds = runtime != null ? runtime.stateSeconds : legacy != null ? legacy.stateSeconds : 0f;
                if (slot.Plant != null) slot.Plant.ShowSimulatedState(ToActivity(state), seconds);
                else if (slot.Ranged != null && equip != null) ShowRanged(slot.Ranged, runtime, service.Owned(equip.instanceId).Stats);
            }
        }

        private static void ShowRanged(DefensePlantAnimation plant, BasementPlantRuntime runtime, BasementPlantStats stats)
        {
            var state = runtime != null ? runtime.state : BasementPlantState.Ready;
            float seconds = runtime != null ? runtime.stateSeconds : 0f;
            if (stats.Attack == BasementPlantAttack.PetalMagazine)
            {
                int magazine = Mathf.Max(1, stats.Magazine);
                int ammo = runtime == null || runtime.ammo < 0 ? magazine : runtime.ammo;
                plant.ShowMagazine(ammo, magazine, state == BasementPlantState.Reloading, seconds, stats.ReloadSeconds,
                    runtime != null ? runtime.shotTimer : 0f, stats.CooldownSeconds);
            }
            else plant.ShowStrike(state == BasementPlantState.Attacking, seconds, stats.StrikeSpeed);
        }

        // ---------- Needles, petals and electric streams (simulated; shown only) ----------

        private void RefreshShots(BasementDefenseData data, BasementDefenseSimulation simulation)
        {
            liveShots.Clear();
            foreach (var shot in data.projectiles)
            {
                liveShots.Add(shot.id);
                if (!shots.TryGetValue(shot.id, out var view))
                {
                    bool petal = shot.kind == BasementPlantAttack.PetalMagazine;
                    var sprite = petal ? petalSprite : needleSprite;
                    if (sprite == null) continue;
                    var go = new GameObject((petal ? "Petal " : "Needle ") + shot.id);
                    go.transform.SetParent(ratLayer != null ? ratLayer : transform, true);
                    go.transform.localScale = Vector3.one; // World-sized below, whatever the room scale.
                    go.transform.localScale = WorldSized(go.transform.parent, petal ? petalScale : needleScale);
                    view = go.AddComponent<SpriteRenderer>();
                    view.sprite = sprite;
                    view.sortingOrder = shotSortingOrder;
                    shots.Add(shot.id, view);
                }
                var rat = data.rats.Find(candidate => candidate.id == shot.ratId);
                var position = new Vector2(shot.x, shot.y);
                view.transform.position = new Vector3(position.x, position.y, depth);
                if (rat == null) continue;
                // The sprite points up: turn it towards where its rat is now.
                Vector2 heading = simulation.RatPosition(rat) - position;
                if (heading.sqrMagnitude > 1e-6f)
                    view.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg - 90f);
            }
            staleShots.Clear();
            foreach (var pair in shots) if (!liveShots.Contains(pair.Key)) staleShots.Add(pair.Key);
            foreach (int id in staleShots)
            {
                if (shots[id] != null) Destroy(shots[id].gameObject);
                shots.Remove(id);
            }
        }

        private static Vector3 WorldSized(Transform parent, float size)
        {
            var lossy = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(size / Mathf.Max(1e-4f, Mathf.Abs(lossy.x)), size / Mathf.Max(1e-4f, Mathf.Abs(lossy.y)), 1f);
        }

        private void RefreshArcs(BasementDefenseData data, BasementDefenseSimulation simulation)
        {
            liveShots.Clear();
            float visible = Mathf.Max(.05f, service.Settings.ArcVisibleSeconds);
            foreach (var arc in data.arcs)
            {
                liveShots.Add(arc.id);
                if (!arcs.TryGetValue(arc.id, out var line))
                {
                    var go = new GameObject("Electric Stream " + arc.id);
                    go.transform.SetParent(ratLayer != null ? ratLayer : transform, false);
                    line = go.AddComponent<LineRenderer>();
                    line.useWorldSpace = true;
                    line.sharedMaterial = arcMaterial;
                    line.sortingOrder = shotSortingOrder;
                    line.textureMode = LineTextureMode.Stretch;
                    line.numCapVertices = 2;
                    line.positionCount = arcSegments + 1;
                    if (arcGlowWidth > 0f)
                    {
                        var glow = new GameObject("Glow").AddComponent<LineRenderer>();
                        glow.transform.SetParent(go.transform, false);
                        glow.useWorldSpace = true;
                        glow.sharedMaterial = arcMaterial;
                        glow.sortingOrder = shotSortingOrder - 1;
                        glow.numCapVertices = 4;
                        glow.positionCount = arcSegments + 1;
                    }
                    arcs.Add(arc.id, line);
                }
                // From the plant's live muzzle (if that plant still stands there) to the rat's live position, or
                // to where it was hit if the hit killed it.
                Vector3 from = new Vector3(arc.fromX, arc.fromY, depth);
                var slot = FindSlot(arc.slotId);
                if (slot != null && slot.Ranged != null && slot.PlantInstanceId == arc.sourceInstanceId &&
                    slot.Ranged.TryGetMuzzlePosition(out var muzzle)) from = new Vector3(muzzle.x, muzzle.y, depth);
                var rat = data.rats.Find(candidate => candidate.id == arc.ratId);
                Vector2 end = rat != null ? simulation.RatPosition(rat) : new Vector2(arc.toX, arc.toY);
                Vector3 to = new Vector3(end.x, end.y, depth);
                float fade = 1f - Mathf.Clamp01(arc.age / visible);
                var color = arcColor;
                color.a *= fade;
                line.startColor = line.endColor = color;
                line.startWidth = arcWidth;
                line.endWidth = arcWidth * .6f;
                Vector3 side = Vector3.Cross(to - from, Vector3.forward).normalized;
                for (int index = 0; index <= arcSegments; index++)
                {
                    float t = index / (float)arcSegments;
                    float bend = index == 0 || index == arcSegments ? 0f : Random.Range(-arcJitter, arcJitter);
                    line.SetPosition(index, Vector3.Lerp(from, to, t) + side * bend);
                }
                var halo = line.transform.childCount > 0 ? line.transform.GetChild(0).GetComponent<LineRenderer>() : null;
                if (halo != null)
                {
                    var soft = color;
                    soft.a *= arcGlowAlpha;
                    halo.startColor = halo.endColor = soft;
                    halo.startWidth = arcWidth * arcGlowWidth;
                    halo.endWidth = arcWidth * arcGlowWidth * .6f;
                    for (int index = 0; index <= arcSegments; index++) halo.SetPosition(index, line.GetPosition(index));
                }
            }
            staleShots.Clear();
            foreach (var pair in arcs) if (!liveShots.Contains(pair.Key)) staleShots.Add(pair.Key);
            foreach (int id in staleShots)
            {
                if (arcs[id] != null) Destroy(arcs[id].gameObject);
                arcs.Remove(id);
            }
        }

        // ---------- Impacts and deaths (from the simulation's hit history; shown once, never replayed) ----------

        private void ShowHits(BasementDefenseSimulation simulation)
        {
            if (hitSource != simulation || hitCursor < 0)
            {
                // A new view or simulation: start from now, so returning to Home does not replay past hits.
                hitSource = simulation;
                hitCursor = simulation.EventSequence;
                return;
            }
            hits.Clear();
            hitCursor = simulation.CopyHitsSince(hitCursor, hits);
            foreach (var hit in hits)
            {
                var at = new Vector3(hit.Position.x, hit.Position.y, depth);
                switch (hit.Attack)
                {
                    case BasementPlantAttack.NeedleVolley: PlayPuff(at); break;
                    case BasementPlantAttack.PetalMagazine:
                        PlayFlipbook(petalImpactFrames, petalImpactFps, at, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)),
                            WorldScale(petalImpactScale), impactSortingOrder);
                        break;
                }
                if (!hit.Killed) continue;
                var view = TakeRatView(hit);
                if (view == null) continue;
                if (hit.Attack == BasementPlantAttack.ElectricArc && electrocutedFrames.Length > 0)
                {
                    // Same place, turn and size as the rat, so the first frame continues it seamlessly.
                    var sprite = view.GetComponent<SpriteRenderer>();
                    PlayFlipbook(electrocutedFrames, electrocutedFps, view.transform.position, view.transform.rotation,
                        view.transform.lossyScale, sprite != null ? sprite.sortingOrder + 1 : impactSortingOrder,
                        sprite != null ? sprite.sortingLayerID : 0);
                    Destroy(view.gameObject);
                }
                else StartFall(view);
            }
        }

        // The killed rat's visual, removed from the live rats (or made now if it died before being shown).
        private BasementRatView TakeRatView(BasementHitEvent hit)
        {
            if (rats.TryGetValue(hit.RatId, out var view))
            {
                rats.Remove(hit.RatId);
                catchStarts.Remove(hit.RatId);
                if (view != null) return view;
            }
            if (ratPrefab == null) return null;
            view = Instantiate(ratPrefab, ratLayer != null ? ratLayer : transform, true);
            view.name = "Rat " + hit.RatId;
            view.Place(new Vector3(hit.Position.x, hit.Position.y, depth), path.Direction(hit.PathFraction));
            return view;
        }

        private void StartFall(BasementRatView view)
        {
            view.name += " (falling)";
            var animator = view.GetComponent<Animator>();
            if (animator != null) animator.speed = 0f; // Limp.
            var sprite = view.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                EnsureGroundMask(sprite);
                sprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // Hidden once below the floor.
            }
            falling.Add(new FallingRat
            {
                View = view, Sprite = sprite,
                Velocity = new Vector3(Random.Range(-.4f, .4f), 1f, 0f) * fallKickMetres * WorldPerMetre,
                Spin = fallSpinDegrees * (Random.value < .5f ? -1f : 1f),
                Landed = sprite != null && sprite.bounds.min.y <= GroundY // Already at the floor: just sink.
            });
        }

        private float GroundY
        {
            get
            {
                if (ratGround != null) return ratGround.position.y;
                float lowest = float.MaxValue; // No floor set: the lowest point of the pipe.
                for (int index = 0; index <= 64; index++) lowest = Mathf.Min(lowest, path.Evaluate(index / 64f).y);
                return lowest;
            }
        }

        // Everything above the floor line shows the falling rats; below it they are cut away.
        private void EnsureGroundMask(SpriteRenderer rat)
        {
            if (groundMask == null)
            {
                if (maskSprite == null)
                    maskSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(.5f, 0f), 4f);
                var go = new GameObject("Rat Ground Mask");
                go.transform.SetParent(ratLayer != null ? ratLayer : transform, false);
                groundMask = go.AddComponent<SpriteMask>();
                groundMask.sprite = maskSprite;
                groundMask.isCustomRangeActive = true; // Only rats are affected.
            }
            groundMask.frontSortingLayerID = groundMask.backSortingLayerID = rat.sortingLayerID;
            // Range (back, front] around the rat's own order; only dead rats use VisibleInsideMask.
            groundMask.frontSortingOrder = rat.sortingOrder + 1;
            groundMask.backSortingOrder = rat.sortingOrder - 1;
            groundMask.transform.position = new Vector3(transform.position.x, GroundY, depth);
            groundMask.transform.localScale = Vector3.one;
            groundMask.transform.localScale = WorldSized(groundMask.transform.parent, 100000f);
        }

        private void AnimateEffects(float dt)
        {
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, .05f); // A stalled frame slows the effect instead of skipping it.
            for (int index = flipbooks.Count - 1; index >= 0; index--)
            {
                var book = flipbooks[index];
                book.Age += dt;
                int frame = Mathf.FloorToInt(book.Age * book.Fps);
                if (book.Renderer == null || frame >= book.Frames.Length)
                {
                    if (book.Renderer != null) { book.Renderer.gameObject.SetActive(false); flipbookPool.Push(book.Renderer); }
                    flipbooks.RemoveAt(index);
                    continue;
                }
                book.Renderer.sprite = book.Frames[frame];
            }

            for (int index = puffs.Count - 1; index >= 0; index--)
            {
                if (puffs[index].effect != null && Time.unscaledTime < puffs[index].until) continue;
                if (puffs[index].effect != null) Destroy(puffs[index].effect);
                puffs.RemoveAt(index);
            }

            if (falling.Count == 0) return;
            float ground = GroundY, metre = WorldPerMetre;
            for (int index = falling.Count - 1; index >= 0; index--)
            {
                var rat = falling[index];
                if (rat.View == null) { falling.RemoveAt(index); continue; }
                var t = rat.View.transform;
                if (!rat.Landed)
                {
                    rat.Velocity += Vector3.down * fallGravityMetres * metre * dt;
                    t.position += rat.Velocity * dt;
                    t.rotation *= Quaternion.Euler(0f, 0f, rat.Spin * dt);
                    float bottom = rat.Sprite != null ? rat.Sprite.bounds.min.y : t.position.y;
                    if (bottom <= ground && rat.Velocity.y <= 0f)
                    {
                        // Touches the floor (never passes it in one long frame), then sinks in.
                        t.position += Vector3.up * (ground - bottom);
                        rat.Landed = true;
                        continue;
                    }
                }
                else t.position += Vector3.down * sinkMetres * metre * dt;
                float top = rat.Sprite != null ? rat.Sprite.bounds.max.y : t.position.y;
                if (rat.Landed && top < ground)
                {
                    Destroy(rat.View.gameObject);
                    falling.RemoveAt(index);
                }
            }
        }

        private void PlayFlipbook(Sprite[] frames, float fps, Vector3 position, Quaternion rotation, Vector3 worldScale, int order, int layer = 0)
        {
            if (frames == null || frames.Length == 0) return;
            var renderer = flipbookPool.Count > 0 ? flipbookPool.Pop() : null;
            if (renderer == null)
            {
                renderer = new GameObject("Effect").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(ratLayer != null ? ratLayer : transform, false);
            }
            renderer.gameObject.SetActive(true);
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
            renderer.sprite = frames[0];
            var t = renderer.transform;
            t.SetPositionAndRotation(position, rotation);
            t.localScale = Vector3.one;
            var lossy = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(worldScale.x / Mathf.Max(1e-4f, Mathf.Abs(lossy.x)), worldScale.y / Mathf.Max(1e-4f, Mathf.Abs(lossy.y)), 1f);
            flipbooks.Add(new Flipbook { Renderer = renderer, Frames = frames, Fps = fps });
        }

        private void PlayPuff(Vector3 position)
        {
            if (needleImpact == null) return;
            var effect = Instantiate(needleImpact, ratLayer != null ? ratLayer : transform, false);
            effect.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            // VFX_Puff scales in Local mode (its own transform only), so this is its world size factor.
            effect.transform.localScale = Vector3.one * needleImpactScale;
            foreach (var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true)) renderer.sortingOrder = impactSortingOrder;
            foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true)) system.Play(false);
            puffs.Add((effect, Time.unscaledTime + needleImpactSeconds));
        }

        private static Vector3 WorldScale(float size) => new Vector3(size, size, 1f);

        private void ClearEffects()
        {
            foreach (var book in flipbooks) if (book.Renderer != null) Destroy(book.Renderer.gameObject);
            flipbooks.Clear();
            while (flipbookPool.Count > 0) { var pooled = flipbookPool.Pop(); if (pooled != null) Destroy(pooled.gameObject); }
            foreach (var puff in puffs) if (puff.effect != null) Destroy(puff.effect);
            puffs.Clear();
            foreach (var rat in falling) if (rat.View != null) Destroy(rat.View.gameObject);
            falling.Clear();
            hitCursor = -1; // Re-enabled later: start from that moment.
        }

        private void OnDestroy()
        {
            if (maskSprite != null) Destroy(maskSprite);
        }

        private void OnDrawGizmosSelected()
        {
            if (gizmoRangeMetres <= 0f) return;
            Gizmos.color = new Color(.4f, .85f, 1f, .7f);
            float radius = gizmoRangeMetres * WorldPerMetre;
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                var center = slot.PlantAnchor.position;
                for (int index = 0; index < 64; index++)
                {
                    float a = index / 64f * Mathf.PI * 2f, b = (index + 1) / 64f * Mathf.PI * 2f;
                    Gizmos.DrawLine(center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                        center + new Vector3(Mathf.Cos(b), Mathf.Sin(b)) * radius);
                }
            }
        }

        private readonly Dictionary<BasementPlantSlot, string> labels = new Dictionary<BasementPlantSlot, string>();

        private void SetLabel(BasementPlantSlot slot, string label)
        {
            if (slot.ProtectionButton == null || (labels.TryGetValue(slot, out var shown) && shown == label)) return;
            labels[slot] = label;
            slot.ProtectionButton.Label = label;
        }

        private static RatEater.Activity ToActivity(BasementPlantState state) => state switch
        {
            BasementPlantState.Eating => RatEater.Activity.Eating,
            BasementPlantState.Cooldown => RatEater.Activity.Cooldown,
            _ => RatEater.Activity.Idle
        };

        private void RefreshRats(BasementDefenseData data)
        {
            float travel = service.Settings.TravelSeconds;
            float bite = Mathf.Max(.01f, service.Settings.BiteSeconds);
            foreach (var rat in data.rats)
            {
                if (!rats.TryGetValue(rat.id, out var view))
                {
                    if (ratPrefab == null) continue;
                    // World-space instantiate: the prefab's scale is the rat's real size, whatever the room scale.
                    view = Instantiate(ratPrefab, ratLayer != null ? ratLayer : transform, true);
                    view.name = "Rat " + rat.id;
                    view.RatId = rat.id;
                    rats.Add(rat.id, view);
                }

                float fraction = rat.travelled / travel;
                Vector3 onPipe = path.Evaluate(fraction);
                view.ShowSpeed(rat.IsCaught ? 1f : rat.SpeedMultiplier);
                if (!rat.IsCaught)
                {
                    catchStarts.Remove(rat.id);
                    view.Place(onPipe, path.Direction(fraction));
                    continue;
                }

                // Caught: glide from where it was caught into the live mouth, arriving as the jaws close.
                var slot = FindSlot(rat.caughtBy);
                float eatSeconds = EatSecondsOf(data, rat.caughtBy);
                if (!catchStarts.TryGetValue(rat.id, out var start)) catchStarts[rat.id] = start = onPipe;
                Vector3 mouth = slot != null && slot.Plant != null && slot.Plant.TryGetMouthPosition(out var live)
                    ? live : slot != null ? slot.MouthCenter : onPipe;
                float glide = Mathf.Max(.01f, bite - rat.caughtAt); // Rats joining a running Eat glide faster.
                float t = eatSeconds >= 0f ? Mathf.Clamp01((eatSeconds - rat.caughtAt) / glide) : 1f;
                float eased = t * t * (3f - 2f * t);
                var position = Vector3.Lerp(start, new Vector3(mouth.x, mouth.y, start.z), eased);
                Vector2 heading = (Vector2)(mouth - position);
                view.Place(position, heading.sqrMagnitude > .0004f ? heading : path.Direction(fraction));
            }

            staleRats.Clear();
            foreach (var pair in rats)
                if (data.rats.Find(rat => rat.id == pair.Key) == null) staleRats.Add(pair.Key);
            foreach (int id in staleRats)
            {
                if (rats[id] != null) Destroy(rats[id].gameObject);
                rats.Remove(id);
                catchStarts.Remove(id);
            }
        }

        // Eat progress of the plant on a shelf, or −1 when it is not eating.
        private static float EatSecondsOf(BasementDefenseData data, string slotId)
        {
            var equip = data.Equip(slotId);
            var runtime = equip != null ? data.Runtime(equip.instanceId) : null;
            if (runtime != null) return runtime.state == BasementPlantState.Eating ? runtime.stateSeconds : -1f;
            var legacy = data.Legacy(slotId);
            return legacy != null && legacy.state == BasementPlantState.Eating ? legacy.stateSeconds : -1f;
        }

        private BasementPlantSlot FindSlot(string slotId)
        {
            foreach (var slot in slots)
                if (slot != null && slot.SlotId == slotId) return slot;
            return null;
        }

        private void Repair()
        {
            if (service == null) return;
            var result = service.TryRepair(); // Synchronous: a second click sees the generator already repaired.
            if (result == BasementRepairResult.NotEnoughGold) ShowMessage(notEnoughGoldMessage);
            else if (result == BasementRepairResult.Unavailable) ShowMessage(unavailableMessage);
            Refresh();
        }

        private void ShowMessage(string text)
        {
            if (repairMessage == null) return;
            repairMessage.text = text;
            repairMessage.gameObject.SetActive(true);
            messageUntil = Time.unscaledTime + messageSeconds;
        }
    }
}
