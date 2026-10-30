using System;

namespace DoctorHelp
{
    // Session time only: never advances while suspended and never writes a checkpoint.
    public sealed class ClinicClock
    {
        public const int OpeningMinute = 8 * 60;
        public const int ClosingMinute = 21 * 60;
        public double ElapsedSeconds { get; private set; }
        public double OperatingSeconds { get; private set; } = 720;
        public bool IsRunning { get; private set; }
        public bool IsClosed => ElapsedSeconds >= OperatingSeconds;
        public int MinuteOfDay => Math.Min(ClosingMinute, OpeningMinute +
            (int)Math.Floor(ElapsedSeconds / OperatingSeconds * (ClosingMinute - OpeningMinute)));
        public string DisplayTime => $"{MinuteOfDay / 60:00}:{MinuteOfDay % 60:00}";

        public void Open(double operatingSeconds)
        {
            if (double.IsNaN(operatingSeconds) || double.IsInfinity(operatingSeconds) || operatingSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(operatingSeconds));
            OperatingSeconds = operatingSeconds;
            ElapsedSeconds = 0;
            IsRunning = true;
        }

        public void Tick(double seconds, bool suspended)
        {
            if (!IsRunning || suspended || seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            ElapsedSeconds = Math.Min(OperatingSeconds, ElapsedSeconds + seconds);
        }

        public void Stop() => IsRunning = false;
    }
}
