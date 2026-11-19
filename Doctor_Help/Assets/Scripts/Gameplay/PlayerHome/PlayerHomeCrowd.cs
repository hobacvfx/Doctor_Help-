using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PlayerHomeCrowd : MonoBehaviour
    {
        [SerializeField] private PlayerHomeElevator elevator;
        [SerializeField] private BoxCollider2D streetFloor;
        [SerializeField] private BoxCollider2D streetLeftWall;
        [SerializeField] private BoxCollider2D streetRightWall;
        [SerializeField, Min(0)] private int streetNpcCount = 10;
        [SerializeField] private StreetRockThrowing streetRockThrowing;
        [SerializeField] private PlayerHomeDirt clinicDirt;
        [SerializeField] private ClinicRankingsCatalog clinicRankings;
        [SerializeField] private bool useProfileGhostCount = true;
        [SerializeField, Range(0, 10)] private int fixedGhostCount;
        [SerializeField] private NpcCatalog catalog;
        [SerializeField] private RuntimeAnimatorController locomotion;
        [SerializeField] private Shader examinationShader;
        [SerializeField] private Material walkingSpriteMaterial;
        [SerializeField] private GhostDialogue ghostPrefab;
        [SerializeField] private PlayerHomeReceptionist receptionist;
        [SerializeField] private ClinicFurnitureSlot receptionistSlot;
        [SerializeField, Min(1f)] private float elevatorInterval = 30f;
        [SerializeField] private float npcHeightAboveFloor = 3.9f;
        [SerializeField] private float npcDepth = 0.44f;

        private readonly List<GameObject> unusedNpcPrefabs = new List<GameObject>();
        private readonly List<PlayerHomeWalker> walkers = new List<PlayerHomeWalker>();
        private readonly Stack<PlayerHomeWalker> inactiveVisitors = new Stack<PlayerHomeWalker>();
        private readonly List<PlayerHomeWalker> ghosts = new List<PlayerHomeWalker>();
        private readonly List<PlayerHomeWalker> candidates = new List<PlayerHomeWalker>();
        private PlayerProfileService profile;
        private int wantedCount;
        private int wantedGhostCount;
        private float nextElevatorCall;
        private float nextGhostSpeech = float.PositiveInfinity;
        private GhostDialogue speaker;
        private float? playerPreviousX;
        private bool IsStreet => streetFloor != null;
        internal IReadOnlyList<PlayerHomeWalker> StreetWalkers => walkers;

        private void Start()
        {
            if (catalog != null && catalog.prefabs != null)
                foreach (var prefab in catalog.prefabs)
                    if (prefab != null && prefab.GetComponentInChildren<Animator>(true) != null && !unusedNpcPrefabs.Contains(prefab))
                        unusedNpcPrefabs.Add(prefab);

            bool layoutConfigured = IsStreet ? streetLeftWall != null && streetRightWall != null :
                elevator != null && elevator.isActiveAndEnabled;
            if (!layoutConfigured || locomotion == null || unusedNpcPrefabs.Count == 0)
            {
                Debug.LogError("Crowd requires a configured walking area, a locomotion controller and NPC prefabs.", this);
                enabled = false;
                return;
            }

            profile = PlayerProfileService.Instance;
            if (!IsStreet && profile != null)
                profile.Changed += RefreshCount;
            if (!IsStreet && ghostPrefab == null && (useProfileGhostCount || fixedGhostCount > 0))
                Debug.LogError("Indoor crowd needs a Ghost prefab.", this);
            nextElevatorCall = Time.time + elevatorInterval;
            RefreshCount();
            if (clinicDirt != null)
                clinicDirt.Initialize(profile, elevator, wantedCount);
        }

        private void RefreshCount()
        {
            if (IsStreet)
            {
                wantedCount = Mathf.Max(0, streetNpcCount);
                wantedGhostCount = 0;
                return;
            }

            bool hasProfile = profile != null && profile.IsLoaded;
            float rating = clinicRankings != null
                ? clinicRankings.GetClinicRating(gameObject.scene.name, profile)
                : hasProfile ? profile.Rating : PlayerProfileData.DefaultRating;
            wantedCount = ClinicDemand.IndoorVisitors(rating);
            if (clinicDirt != null && hasProfile)
            {
                wantedCount += profile.ClinicSignLevel;
                wantedCount = Mathf.Min(wantedCount, walkers.Count + inactiveVisitors.Count + unusedNpcPrefabs.Count);
                clinicDirt.UpdateVisitorCount(wantedCount);
            }
            wantedGhostCount = useProfileGhostCount ? (hasProfile ? profile.GhostNumber : 0) : Mathf.Clamp(fixedGhostCount, 0, 10);
        }

        private void Update()
        {
            UpdatePopulation(walkers, clinicDirt != null ? clinicDirt.CrowdSize(wantedCount) : wantedCount, isGhost: false);
            ObservePlayerAtReception();
            if (ghostPrefab != null)
                UpdatePopulation(ghosts, wantedGhostCount, isGhost: true);
            UpdateGhostSpeech();

            if (IsStreet || Time.time < nextElevatorCall || !elevator.IsReady)
                return;
            candidates.Clear();
            AddDockedCandidates(walkers);
            AddDockedCandidates(ghosts);

            if (candidates.Count == 0)
            {
                candidates.AddRange(walkers);
                candidates.AddRange(ghosts);
            }
            if (candidates.Count > 0 && elevator.TryDispatch(candidates[Random.Range(0, candidates.Count)]))
                nextElevatorCall = Time.time + elevatorInterval;
        }

        private void AddDockedCandidates(List<PlayerHomeWalker> population)
        {
            foreach (var walker in population)
                if (walker.Floor == elevator.DockedFloor)
                    candidates.Add(walker);
        }

        private void UpdatePopulation(List<PlayerHomeWalker> population, int count, bool isGhost)
        {
            var currentReceptionist = CurrentReceptionist;
            for (int i = population.Count - 1; i >= 0; i--)
            {
                var walker = population[i];
                if (walker.Transform == null)
                {
                    population.RemoveAt(i);
                    if (!isGhost && clinicDirt != null) clinicDirt.Forget(walker);
                }
                else if (population.Count > count && (IsStreet || !elevator.IsServing(walker)))
                {
                    population.RemoveAt(i);
                    if (isGhost) Destroy(walker.Transform.gameObject);
                    else
                    {
                        walker.Transform.gameObject.SetActive(false);
                        inactiveVisitors.Push(walker);
                    }
                }
            }

            while (!isGhost && population.Count < count && inactiveVisitors.Count > 0)
            {
                var walker = inactiveVisitors.Pop();
                if (walker.Transform == null)
                {
                    if (clinicDirt != null) clinicDirt.Forget(walker);
                    continue;
                }
                walker.Transform.gameObject.SetActive(true);
                walker.ResumeWandering();
                population.Add(walker);
            }

            while (population.Count < count && (isGhost || unusedNpcPrefabs.Count > 0))
            {
                int floor = IsStreet ? 0 : (population.Count == 1 ? 1 - population[0].Floor : Random.Range(0, 2));
                var range = WalkingRange(floor);
                float floorHeight = IsStreet ? streetFloor.bounds.max.y : elevator.FloorHeight(floor);
                var point = new Vector3(Random.Range(range.x, range.y),
                    floorHeight + npcHeightAboveFloor, npcDepth);
                int prefabIndex = isGhost ? -1 : Random.Range(0, unusedNpcPrefabs.Count);
                var prefab = isGhost ? ghostPrefab.gameObject : unusedNpcPrefabs[prefabIndex];
                var npc = Instantiate(prefab, point, Quaternion.identity, transform);
                if (!npc.TryGetComponent<SortingGroup>(out _))
                    npc.AddComponent<SortingGroup>().sortingOrder = 5;
                if (!isGhost)
                {
                    unusedNpcPrefabs.RemoveAt(prefabIndex);
                    var animation = npc.AddComponent<PlayerHomeCharacterAnimation>();
                    animation.Configure(locomotion, examinationShader, walkingSpriteMaterial);
                    if (IsStreet && streetRockThrowing != null)
                        streetRockThrowing.ConfigureNpc(animation);
                }
                var walker = new PlayerHomeWalker(npc.transform, floor, WalkingRange);
                population.Add(walker);
                if (!isGhost && clinicDirt != null) clinicDirt.Register(walker);
            }

            foreach (var walker in population)
            {
                float previousX = walker.Transform.position.x;
                if (isGhost || streetRockThrowing == null || !streetRockThrowing.TickNpc(walker))
                    walker.Tick(Time.deltaTime, Time.time);
                if (!isGhost && currentReceptionist != null)
                    currentReceptionist.ObserveWalk(walker.Floor, previousX, walker.Transform.position.x);
                if (!isGhost && clinicDirt != null) clinicDirt.Observe(walker, previousX);
            }
        }

        private PlayerHomeReceptionist CurrentReceptionist
        {
            get
            {
                var equipped = receptionistSlot != null ? receptionistSlot.EquippedVisual : null;
                return equipped != null ? equipped.GetComponent<PlayerHomeReceptionist>() : receptionist;
            }
        }

        // Only the player's own clinic (the one with an upgradable receptionist slot) greets the doctor.
        private void ObservePlayerAtReception()
        {
            var player = !IsStreet && receptionistSlot != null && elevator != null ? elevator.Rider : null;
            if (player == null || !player.isActiveAndEnabled || player.IsRiding)
            {
                playerPreviousX = null;
                return;
            }

            float x = player.Position.x;
            var currentReceptionist = CurrentReceptionist;
            if (playerPreviousX is float previousX && currentReceptionist != null)
            {
                float feet = player.FeetY;
                int floor = Mathf.Abs(feet - elevator.FloorHeight(0)) <= Mathf.Abs(feet - elevator.FloorHeight(1)) ? 0 : 1;
                currentReceptionist.ObservePlayer(floor, previousX, x);
            }
            playerPreviousX = x;
        }

        private Vector2 WalkingRange(int floor)
        {
            if (!IsStreet) return elevator.WalkingRange(floor);
            var ground = streetFloor.bounds;
            return new Vector2(Mathf.Max(ground.min.x, streetLeftWall.bounds.max.x) + 1f,
                Mathf.Min(ground.max.x, streetRightWall.bounds.min.x) - 1f);
        }

        private void UpdateGhostSpeech()
        {
            if (ghosts.Count == 0)
            {
                nextGhostSpeech = float.PositiveInfinity;
                return;
            }
            if (speaker != null && speaker.IsSpeaking)
                return;

            if (float.IsPositiveInfinity(nextGhostSpeech))
                nextGhostSpeech = Time.time + Random.Range(15f, 30f);
            if (Time.time < nextGhostSpeech)
                return;

            speaker = ghosts[Random.Range(0, ghosts.Count)].Transform.GetComponent<GhostDialogue>();
            speaker.Speak();
            nextGhostSpeech = float.PositiveInfinity;
        }

        private void OnDestroy()
        {
            if (profile != null)
                profile.Changed -= RefreshCount;
        }
    }

    internal sealed class PlayerHomeWalker
    {
        internal Transform Transform { get; }
        internal PlayerHomeCharacterAnimation Animation { get; }
        internal int Floor { get; set; }
        internal bool HasArrived => Mathf.Abs(Transform.position.x - destinationX) < 0.01f;

        private readonly Func<int, Vector2> walkingRange;
        private readonly float speed = Random.Range(3.5f, 5.5f);
        private float destinationX;
        private float resumeAt;
        private bool walking;
        private bool usingElevator;

        internal PlayerHomeWalker(Transform transform, int floor, Func<int, Vector2> walkingRange)
        {
            Transform = transform;
            Animation = transform.GetComponent<PlayerHomeCharacterAnimation>();
            Floor = floor;
            this.walkingRange = walkingRange;
            PickDestination();
        }

        internal void Tick(float deltaTime, float now)
        {
            if (walking)
            {
                var position = Transform.position;
                position.x = Mathf.MoveTowards(position.x, destinationX, speed * deltaTime);
                Transform.position = position;
                if (!HasArrived) return;
                walking = false;
                resumeAt = now + Random.Range(3f, 10f);
            }

            if (!usingElevator && now >= resumeAt)
                PickDestination();
        }

        internal void WalkForElevator(float targetX)
        {
            usingElevator = true;
            WalkTo(targetX);
        }

        internal void FollowRockTarget(float targetX)
        {
            var range = walkingRange(Floor);
            var position = Transform.position;
            position.x = Mathf.MoveTowards(position.x, Mathf.Clamp(targetX, range.x, range.y),
                speed * Time.deltaTime);
            Transform.position = position;
        }

        internal void ResumeWandering()
        {
            usingElevator = false;
            walking = false;
            resumeAt = Time.time + Random.Range(3f, 10f);
        }

        private void PickDestination()
        {
            var range = walkingRange(Floor);
            WalkTo(Random.Range(range.x, range.y));
        }

        private void WalkTo(float targetX)
        {
            destinationX = targetX;
            walking = true;
        }
    }
}
