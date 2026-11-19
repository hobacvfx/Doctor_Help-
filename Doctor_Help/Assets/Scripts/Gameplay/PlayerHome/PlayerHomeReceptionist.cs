using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NpcDialogue))]
    public sealed class PlayerHomeReceptionist : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private int floor;
        [SerializeField, Min(0.1f)] private float greetingRange = 1.5f;

        private static readonly string[] Greetings =
        {
            "Please get your number here!",
            "Please wait until our Doctor back.",
            "May i help you?"
        };

        private const string PlayerGreeting = "Morning Doctor!";

        private NpcDialogue dialogue;

        private void Awake() => dialogue = GetComponent<NpcDialogue>();

        internal void ObserveWalk(int walkerFloor, float previousX, float currentX)
        {
            if (EntersGreetingRange(walkerFloor, previousX, currentX))
                dialogue.SpeakAmbient(Greetings[Random.Range(0, Greetings.Length)]);
        }

        internal void ObservePlayer(int playerFloor, float previousX, float currentX)
        {
            if (EntersGreetingRange(playerFloor, previousX, currentX))
                dialogue.SpeakAmbient(PlayerGreeting);
        }

        // A turn already in progress is kept; the next crossing can greet again.
        private bool EntersGreetingRange(int walkerFloor, float previousX, float currentX)
        {
            if (!isActiveAndEnabled || walkerFloor != floor || dialogue.IsShowing)
                return false;

            float left = transform.position.x - greetingRange;
            float right = transform.position.x + greetingRange;
            return (previousX < left && currentX >= left) || (previousX > right && currentX <= right);
        }
    }
}
