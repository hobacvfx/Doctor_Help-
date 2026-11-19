using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PlayerHomeDirt : MonoBehaviour
    {
        private const float MinSpacing = 0.5f;
        private const float SameVisitorSpacing = 2f;
        private const int MaxVisibleStains = 28;
        private const int TestVisitorCount = 2;

        [SerializeField] private ParticleSystem smellyEffect;
        [SerializeField] private ParticleSystem bleedingEffect;
        [SerializeField] private ParticleSystem bleedingWallEffect;
        [SerializeField] private Vector3 effectOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private Sprite[] grimeStains;
        [SerializeField] private Sprite[] bloodStains;
        [SerializeField] private Vector2 wallHeightRange = new Vector2(5.2f, 7.1f);
        [SerializeField] private float stainHeightOffset = 1.4f;
        [SerializeField, Min(0.1f)] private float stainSize = 6.4f;
        [SerializeField] private int stainSortingOrder = -5;

        private sealed class Visitor
        {
            internal int index;
            internal ClinicVisitorDirt kind;
            internal ParticleSystem effect;
            internal bool testing;
        }

        private readonly Dictionary<PlayerHomeWalker, Visitor> visitors = new();
        private readonly List<ClinicWallStain> visibleStains = new();
        private readonly List<ClinicWallStain> testStains = new();
        private readonly Dictionary<ClinicWallStain, Transform> decals = new();
        private PlayerProfileService profile;
        private PlayerHomeElevator elevator;
        private ClinicDirtDay today;
        private Transform testDecals;
        private int visibleTestStains;

        private bool IsTesting => today != null && profile.TestNpcSmellyBleed;
        internal bool HasStains => visibleStains.Count > 0;

        internal void Initialize(PlayerProfileService service, PlayerHomeElevator lift, int expectedVisitors)
        {
            if (service == null || !service.IsLoaded || lift == null) return;
            if (smellyEffect == null || bleedingEffect == null || bleedingWallEffect == null ||
                !ValidAtlas(grimeStains) || !ValidAtlas(bloodStains))
            {
                Debug.LogError("Player Home dirt requires the visitor and wall VFX prefabs and two six-sprite atlases.", this);
                return;
            }

            profile = service;
            elevator = lift;
            today = profile.EnsureClinicDirtDay(expectedVisitors);
            foreach (var entry in profile.ClinicDirtDays)
            {
                if (entry.remainingFraction <= 0f) continue;
                foreach (var stain in entry.stains)
                    if (!stain.cleaned) RenderStain(stain, entry.visitors[stain.visitorIndex], false);
            }
            foreach (var entry in profile.ClinicDirtDays)
                if (entry != today && entry.remainingFraction > 0f) RestoreUnseenStains(entry);
        }

        internal void UpdateVisitorCount(int count)
        {
            if (today != null) today = profile.EnsureClinicDirtDay(count);
        }

        internal int CrowdSize(int normalCount)
        {
            if (IsTesting) return Mathf.Max(normalCount, TestVisitorCount);
            if (testDecals != null)
            {
                Destroy(testDecals.gameObject);
                testDecals = null;
                foreach (var stain in testStains)
                {
                    visibleStains.Remove(stain);
                    decals.Remove(stain);
                }
                visibleTestStains = 0;
                testStains.Clear();
            }
            return normalCount;
        }

        internal void Register(PlayerHomeWalker walker)
        {
            if (today == null) return;
            int index = 0;
            while (IsAssigned(index)) index++;
            visitors.Add(walker, new Visitor { index = index });
        }

        internal void Forget(PlayerHomeWalker walker) => visitors.Remove(walker);

        internal void Observe(PlayerHomeWalker walker, float previousX)
        {
            if (!visitors.TryGetValue(walker, out var visitor)) return;
            bool testing = IsTesting && visitor.index < TestVisitorCount;
            var kind = testing ? (visitor.index == 0 ? ClinicVisitorDirt.Smelly : ClinicVisitorDirt.Bleeding) :
                visitor.index < today.visitors.Count ? today.visitors[visitor.index] : ClinicVisitorDirt.None;
            UpdateEffect(walker, visitor, kind, testing);
            if (kind == ClinicVisitorDirt.None ||
                elevator.IsServing(walker) || Mathf.Abs(previousX - walker.Transform.position.x) < 0.001f) return;

            if (TryLeaveStain(testing ? testStains : today.stains, visitor.index, kind, walker.Floor,
                walker.Transform.position.x, testing) && !testing)
                today.UpdateRemainingStains();
        }

        internal ClinicWallStain ClosestStain(int floor, float x)
        {
            ClinicWallStain best = null;
            foreach (var stain in visibleStains)
            {
                if (best == null ||
                    (stain.floor == floor && best.floor != floor) ||
                    ((stain.floor == floor) == (best.floor == floor) && Mathf.Abs(stain.x - x) < Mathf.Abs(best.x - x)))
                    best = stain;
            }
            return best;
        }

        internal bool TryGetStainPosition(ClinicWallStain stain, out Vector3 position)
        {
            position = default;
            if (stain == null || !decals.TryGetValue(stain, out var decal) || decal == null) return false;
            position = decal.position;
            return true;
        }

        internal ClinicWallStain FindVisibleStain(Camera camera, int floor, float playerX)
        {
            ClinicWallStain nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var stain in visibleStains)
            {
                float difference = Mathf.Abs(stain.x - playerX);
                if (stain.floor != floor || difference >= distance || !TryGetScreenBounds(stain, camera, out _)) continue;
                distance = difference;
                nearest = stain;
            }
            return nearest;
        }

        internal bool TryGetScreenBounds(ClinicWallStain stain, Camera camera, out Rect result)
        {
            result = default;
            if (stain == null || camera == null || !decals.TryGetValue(stain, out var decal) || decal == null) return false;
            var visual = decal.GetComponentInChildren<SpriteRenderer>();
            if (visual == null || !visual.enabled || !visual.gameObject.activeInHierarchy) return false;
            Bounds bounds = visual.bounds;
            Vector3 min = camera.WorldToScreenPoint(bounds.min);
            Vector3 max = camera.WorldToScreenPoint(bounds.max);
            if (min.z <= 0 || max.z <= 0) return false;
            var viewport = camera.pixelRect;
            if (!viewport.Contains(((Vector2)min + (Vector2)max) * .5f)) return false;
            result = Rect.MinMaxRect(Mathf.Max(viewport.xMin, min.x), Mathf.Max(viewport.yMin, min.y),
                Mathf.Min(viewport.xMax, max.x), Mathf.Min(viewport.yMax, max.y));
            return result.width > 0 && result.height > 0;
        }

        internal void CleanStain(ClinicWallStain stain)
        {
            if (stain == null || !decals.Remove(stain, out var decal)) return;
            visibleStains.Remove(stain);
            stain.cleaned = true;
            if (decal != null)
            {
                decal.gameObject.SetActive(false);
                Destroy(decal.gameObject);
            }
            if (testStains.Contains(stain))
            {
                visibleTestStains--;
                return;
            }
            foreach (var entry in profile.ClinicDirtDays)
            {
                if (entry.stains.Contains(stain)) entry.UpdateRemainingStains();
                if (entry != today && entry.remainingFraction > 0f) RestoreUnseenStains(entry);
            }
        }

        private void UpdateEffect(PlayerHomeWalker walker, Visitor visitor, ClinicVisitorDirt kind, bool testing)
        {
            if (visitor.kind == kind && visitor.testing == testing)
            {
                if (visitor.effect != null && !visitor.effect.isPlaying) visitor.effect.Play();
                return;
            }
            if (visitor.effect != null)
            {
                visitor.effect.gameObject.SetActive(false);
                Destroy(visitor.effect.gameObject);
                visitor.effect = null;
            }
            visitor.kind = kind;
            visitor.testing = testing;
            if (kind == ClinicVisitorDirt.None) return;
            var prefab = kind == ClinicVisitorDirt.Smelly ? smellyEffect : bleedingEffect;
            visitor.effect = Instantiate(prefab, walker.Transform, false);
            visitor.effect.transform.localPosition = effectOffset;
            visitor.effect.Play();
        }

        private void RestoreUnseenStains(ClinicDirtDay entry)
        {
            for (int index = 0; index < entry.visitors.Count; index++)
            {
                var kind = entry.visitors[index];
                if (kind == ClinicVisitorDirt.None || CountStains(entry.stains, index) > 0) continue;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int floor = Random.Range(0, 2);
                    var range = elevator.WalkingRange(floor);
                    if (TryLeaveStain(entry.stains, index, kind, floor, Random.Range(range.x, range.y), false))
                        break;
                }
            }
        }

        private bool TryLeaveStain(List<ClinicWallStain> entries, int visitor, ClinicVisitorDirt kind,
            int floor, float x, bool testing)
        {
            if ((!testing && visibleStains.Count - visibleTestStains >= MaxVisibleStains) ||
                CountStains(entries, visitor) >= ClinicDirtDay.MaxStainsPerVisitor) return false;
            foreach (var stain in entries)
                if (stain.visitorIndex == visitor && stain.floor == floor && Mathf.Abs(stain.x - x) < SameVisitorSpacing)
                    return false;
            foreach (var stain in visibleStains)
                if (stain.floor == floor && Mathf.Abs(stain.x - x) < MinSpacing) return false;

            var mark = new ClinicWallStain
            {
                visitorIndex = visitor, floor = floor, x = x,
                height = Random.Range(wallHeightRange.x, wallHeightRange.y),
                rotation = Random.Range(-30f, 30f),
                variant = Random.Range(0, ClinicDirtDay.VariantCount)
            };
            entries.Add(mark);
            RenderStain(mark, kind, testing);
            return true;
        }

        private void RenderStain(ClinicWallStain mark, ClinicVisitorDirt kind, bool testing)
        {
            if (testing && testDecals == null)
            {
                testDecals = new GameObject("Test Wall Stains").transform;
                testDecals.SetParent(transform, false);
            }
            bool smelly = kind == ClinicVisitorDirt.Smelly;
            var sprite = (smelly ? grimeStains : bloodStains)[mark.variant];
            var root = new GameObject("Wall Stain").transform;
            root.SetParent(testing ? testDecals : transform, false);
            float wallY = elevator.FloorHeight(mark.floor) + mark.height + stainHeightOffset;
            root.position = new Vector3(mark.x, wallY, 0f);
            var decal = new GameObject("Stain Sprite").AddComponent<SpriteRenderer>();
            decal.transform.SetParent(root, false);
            decal.transform.localRotation = Quaternion.Euler(0f, 0f, mark.rotation);
            var size = sprite.bounds.size;
            decal.transform.localScale = Vector3.one * (stainSize / Mathf.Max(size.x, size.y));
            decal.sprite = sprite;
            decal.sortingOrder = stainSortingOrder;
            var effect = Instantiate(smelly ? smellyEffect : bleedingWallEffect, root, false);
            effect.transform.localPosition = Vector3.zero;
            if (effect.TryGetComponent<ParticleSystemRenderer>(out var effectRenderer))
                effectRenderer.sortingOrder = stainSortingOrder + 1;
            effect.Play();
            visibleStains.Add(mark);
            decals.Add(mark, root);
            if (testing) visibleTestStains++;
        }

        private bool IsAssigned(int index)
        {
            foreach (var visitor in visitors.Values)
                if (visitor.index == index) return true;
            return false;
        }

        private static int CountStains(List<ClinicWallStain> entries, int index)
        {
            int count = 0;
            foreach (var stain in entries)
                if (stain.visitorIndex == index) count++;
            return count;
        }

        private static bool ValidAtlas(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length != ClinicDirtDay.VariantCount) return false;
            foreach (var sprite in sprites)
                if (sprite == null) return false;
            return true;
        }
    }
}
