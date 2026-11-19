using System;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class NeighborhoodView : MonoBehaviour, INeighborhoodView
    {
        [Serializable]
        private sealed class Route
        {
            [SerializeField] internal DeskActionButton button;
            [SerializeField] internal string destination;
            [SerializeField] internal Transform arrival;
            [NonSerialized] internal Action click;

            internal bool IsConfigured => button != null && button.isActiveAndEnabled && button.IsConfigured &&
                !string.IsNullOrWhiteSpace(destination) && destination != nameof(GameScene.Office);
        }

        [SerializeField] private Route[] doors;
        [SerializeField] private PlayerHomeMovement player;
        [SerializeField] private PlayerHomeCameraFollow cameraFollow;
        [SerializeField] private Transform entrance;
        [SerializeField] private DeskActionButton receptionistUpgrade;
        public event Action<string> TravelRequested;
        public bool IsConfigured => isActiveAndEnabled && player != null && cameraFollow != null && entrance != null &&
            doors != null && doors.Length > 0 &&
            Array.TrueForAll(doors, route => route != null && route.IsConfigured);

        internal void PlacePlayerAtEntrance(string origin)
        {
            var route = Array.Find(doors, door => door.destination == origin && door.arrival != null);
            var spawn = route != null ? route.arrival : entrance;
            player.PlaceAtEntrance(spawn.position);
            cameraFollow.SnapToTarget();
        }

        internal DeskActionButton DoorTo(string destination) =>
            Array.Find(doors, door => door.destination == destination)?.button;

        internal void ConfigureFirstDayGuide(FirstDayGuide prefab, DeskActionButton target)
        {
            if (prefab == null || target == null) return;
            var guide = GetComponentInChildren<FirstDayGuide>();
            if (guide == null) guide = Instantiate(prefab, transform);
            var routeHome = DoorTo(nameof(GameScene.Player_Home)) ?? DoorTo("Street");
            guide.Configure(player, target, receptionistUpgrade != null ? receptionistUpgrade : routeHome);
        }

        private void OnEnable()
        {
            if (doors == null) return;
            foreach (var route in doors)
            {
                if (route?.button == null) continue;
                route.click = () => TravelRequested?.Invoke(route.destination);
                route.button.Clicked += route.click;
            }
        }

        private void OnDisable()
        {
            if (doors == null) return;
            foreach (var route in doors)
            {
                if (route?.button == null) continue;
                route.button.Clicked -= route.click;
                route.click = null;
            }
        }

    }
}
