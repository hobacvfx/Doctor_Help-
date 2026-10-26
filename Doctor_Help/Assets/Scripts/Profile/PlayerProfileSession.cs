using System;

namespace DoctorHelp
{
    // The working day is memory-only. Only CompleteDay can create a checkpoint.
    public sealed class PlayerProfileSession
    {
        public PlayerProfileData WorkingProfile { get; private set; }
        private readonly Action<PlayerProfileData> persist;

        public PlayerProfileSession(PlayerProfileData checkpoint, Action<PlayerProfileData> persist)
        {
            if (checkpoint == null || !checkpoint.IsValid())
                throw new ArgumentException("Invalid checkpoint.", nameof(checkpoint));
            this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
            WorkingProfile = checkpoint.Copy();
        }

        public void CompleteDay()
        {
            var nextDay = WorkingProfile.Copy();
            nextDay.currentDay = checked(nextDay.currentDay + 1);
            persist(nextDay.Copy()); // Failure leaves the current day available for retry.
            WorkingProfile = nextDay;
        }
    }
}
