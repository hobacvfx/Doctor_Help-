using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PlayerHomeElevator : MonoBehaviour
    {
        [SerializeField] private PlayerHomeMovement rider;
        [SerializeField] private Transform cabin;
        [SerializeField] private BoxCollider2D platform;
        [SerializeField] private BoxCollider2D lowerFloor;
        [SerializeField] private BoxCollider2D upperFloor;
        [SerializeField] private BoxCollider2D leftWall;
        [SerializeField, Min(0.01f)] private float boardingHalfWidth = 1.1f;
        [SerializeField, Min(0.01f)] private float travelSeconds = 2f;
        [SerializeField, Min(0f)] private float boardingSeconds = 1.25f;

        private enum Phase { Ready, FetchingNpc, BoardingNpc, Riding, UnloadingNpc, Returning }

        private Phase phase;
        private int targetFloor;
        private PlayerHomeWalker passenger;
        private Vector2 riderStart;
        private Vector3 passengerStart;
        private Vector3 cabinStart;
        private Vector3 platformStart;
        private float travelDelta;
        private float elapsed;
        private float? boardUntil;
        private bool mustExitCabin;

        internal bool IsReady => phase == Phase.Ready;
        internal int DockedFloor { get; private set; }
        internal PlayerHomeMovement Rider => rider;
        // The walker inside the moving cabin, or null when the cabin is idle, fetching or carrying only the player.
        internal PlayerHomeWalker RidingPassenger => phase == Phase.Riding ? passenger : null;
        internal float BoardingX => cabin.position.x;
        internal bool IsServing(PlayerHomeWalker walker) => passenger == walker;

        private float ShaftEntrance => platform.bounds.min.x;
        private float ExitX => ShaftEntrance - 1.15f;
        private bool PlayerInShaft => rider.Position.x >= ShaftEntrance;

        internal float FloorHeight(int floor) => (floor == 0 ? lowerFloor : upperFloor).bounds.max.y;

        internal Vector2 WalkingRange(int floor)
        {
            var bounds = (floor == 0 ? lowerFloor : upperFloor).bounds;
            return new Vector2(Mathf.Max(bounds.min.x, leftWall.bounds.max.x) + 1f,
                Mathf.Min(bounds.max.x, ExitX));
        }

        private void Awake()
        {
            if (rider != null && cabin != null && platform != null && lowerFloor != null &&
                upperFloor != null && leftWall != null)
                return;

            Debug.LogError("Player Home elevator needs its rider, cabin, platform, both floors and left wall.", this);
            enabled = false;
        }

        internal bool TryDispatch(PlayerHomeWalker walker)
        {
            if (!IsReady || rider.IsWalkingAutomatically || (walker.Floor != DockedFloor && PlayerInShaft))
                return false;

            passenger = walker;
            if (passenger.Floor == DockedFloor)
                BeginBoarding();
            else
            {
                passenger.WalkForElevator(ExitX);
                BeginTravel(passenger.Floor, Phase.FetchingNpc);
            }
            UpdateShaftAccess();
            return true;
        }

        private void Update()
        {
            if (!rider.isActiveAndEnabled)
                return;

            switch (phase)
            {
                case Phase.Ready:
                    UpdatePlayerBoarding();
                    break;
                case Phase.BoardingNpc:
                    UpdateNpcBoarding();
                    break;
                case Phase.UnloadingNpc:
                    if (passenger.Transform == null || passenger.HasArrived)
                        FinishNpcStop();
                    break;
                default:
                    UpdateTravel();
                    break;
            }
            UpdateShaftAccess();
        }

        private void UpdatePlayerBoarding()
        {
            int playerFloor = PlayerFloor();
            if (playerFloor != DockedFloor)
            {
                BeginTravel(playerFloor, Phase.Returning);
                return;
            }

            if (!PlayerInShaft)
                mustExitCabin = false;

            if (!mustExitCabin &&
                Mathf.Abs(rider.Position.x - cabin.position.x) <= boardingHalfWidth &&
                Mathf.Abs(rider.FeetY - platform.bounds.max.y) <= 0.12f)
            {
                BoardPlayer();
                BeginTravel(1 - DockedFloor, Phase.Riding);
            }
        }

        private void BeginBoarding()
        {
            if (passenger.Transform == null)
            {
                FinishNpcStop();
                return;
            }

            phase = Phase.BoardingNpc;
            boardUntil = null;
            passenger.WalkForElevator(cabin.position.x + boardingHalfWidth * 0.5f);
        }

        private void UpdateNpcBoarding()
        {
            if (passenger.Transform == null)
            {
                FinishNpcStop();
                return;
            }
            if (!passenger.HasArrived)
                return;

            boardUntil ??= Time.time + boardingSeconds;
            if (Time.time < boardUntil)
                return;

            if (PlayerInShaft && PlayerFloor() == DockedFloor)
                BoardPlayer();
            passengerStart = passenger.Transform.position;
            BeginTravel(1 - DockedFloor, Phase.Riding);
        }

        private void BoardPlayer()
        {
            riderStart = rider.Position;
            rider.BeginRide();
            mustExitCabin = true;
        }

        private void BeginTravel(int floor, Phase purpose)
        {
            targetFloor = floor;
            phase = purpose;
            cabinStart = cabin.position;
            platformStart = platform.transform.position;
            travelDelta = FloorHeight(floor) - platform.bounds.max.y;
            elapsed = 0f;
            PlaceDuringTravel(0f);
        }

        private void UpdateTravel()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / travelSeconds);
            PlaceDuringTravel(travelDelta * Mathf.SmoothStep(0f, 1f, progress));
            if (progress < 1f)
                return;

            var arrival = phase;
            DockedFloor = targetFloor;
            Physics2D.SyncTransforms();
            if (rider.IsRiding)
                rider.EndRide();

            switch (arrival)
            {
                case Phase.FetchingNpc:
                    BeginBoarding();
                    break;
                case Phase.Riding when passenger != null && passenger.Transform != null:
                    passenger.Floor = DockedFloor;
                    passenger.WalkForElevator(ExitX);
                    phase = Phase.UnloadingNpc;
                    break;
                case Phase.Riding:
                    FinishNpcStop();
                    break;
                default:
                    phase = Phase.Ready;
                    break;
            }
        }

        private void FinishNpcStop()
        {
            passenger?.ResumeWandering();
            passenger = null;
            int playerFloor = PlayerFloor();
            if (playerFloor != DockedFloor)
                BeginTravel(playerFloor, Phase.Returning);
            else
                phase = Phase.Ready;
        }

        internal int PlayerFloor()
        {
            float feet = rider.FeetY;
            return Mathf.Abs(feet - FloorHeight(0)) <= Mathf.Abs(feet - FloorHeight(1)) ? 0 : 1;
        }

        private void UpdateShaftAccess()
        {
            bool docked = phase == Phase.Ready || phase == Phase.BoardingNpc || phase == Phase.UnloadingNpc;
            bool canEnter = docked && PlayerFloor() == DockedFloor;
            rider.SetShaftEntranceLimit(canEnter ? null : ShaftEntrance);
        }

        private void PlaceDuringTravel(float height)
        {
            platform.transform.position = platformStart + Vector3.up * height;
            cabin.position = cabinStart + Vector3.up * height;
            if (rider.IsRiding)
                rider.PlaceDuringRide(riderStart + Vector2.up * height);
            if (phase == Phase.Riding && passenger != null && passenger.Transform != null)
                passenger.Transform.position = passengerStart + Vector3.up * height;
        }

        private void OnDisable()
        {
            if (rider != null)
            {
                if ((phase == Phase.Riding || phase == Phase.FetchingNpc || phase == Phase.Returning) &&
                    cabin != null && platform != null)
                {
                    PlaceDuringTravel(0f);
                    Physics2D.SyncTransforms();
                }
                if (rider.IsRiding) rider.EndRide();
                rider.SetShaftEntranceLimit(null);
            }
            passenger?.ResumeWandering();
            passenger = null;
            phase = Phase.Ready;
        }
    }
}
