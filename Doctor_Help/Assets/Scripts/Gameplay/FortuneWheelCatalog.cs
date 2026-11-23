using System;
using UnityEngine;

namespace DoctorHelp
{
    [Serializable]
    public sealed class FortuneWheelReward
    {
        public CurrencyKind kind;
        [Min(1)] public int amount = 25;
        public Sprite icon;
        public string Description => $"+{amount} {kind}";
    }

    [CreateAssetMenu(menuName = "Doctor Help/Fortune Wheel")]
    public sealed class FortuneWheelCatalog : ScriptableObject
    {
        public const int SectorCount = 12;
        [SerializeField, Min(1)] private int spinPrice = 70;
        [SerializeField, Min(1)] private float spinSeconds = 5;
        [SerializeField, Min(2)] private int fullTurns = 5;
        [Tooltip("Clockwise from twelve o'clock. Every sector has the same probability (1/12).")]
        [SerializeField] private FortuneWheelReward[] rewards = Array.Empty<FortuneWheelReward>();
        public int SpinPrice => spinPrice;
        public float SpinSeconds => spinSeconds;
        public int FullTurns => fullTurns;
        public FortuneWheelReward[] Rewards => rewards;
        public bool IsConfigured => spinPrice > 0 && spinSeconds >= 1 && fullTurns >= 2 &&
            rewards != null && rewards.Length == SectorCount && Array.TrueForAll(rewards,
                reward => reward != null && reward.amount > 0 && reward.kind >= CurrencyKind.Gold &&
                    reward.kind <= CurrencyKind.Electricity && reward.icon != null);

        internal static float TargetAngle(int index, float offset = 0) => index * 30f + offset;
        internal static int SectorAtPointer(float angle) =>
            Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 30f) % SectorCount;
    }
}
