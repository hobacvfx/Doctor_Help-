using System;

namespace DoctorHelp
{
    internal enum WalterQuestState { Available, Active, ReadyToClaim, Claimed, Failed }

    [Serializable]
    internal struct WalterQuest
    {
        internal const int Target = 600;
        internal const int Days = 7;
        internal const int Reward = 200;

        public WalterQuestState state;
        public int acceptedDay;
        public int earned;

        internal bool Accept(int day)
        {
            if (state != WalterQuestState.Available) return false;
            acceptedDay = day;
            state = WalterQuestState.Active;
            return true;
        }

        internal void RecordEarnings(int amount)
        {
            if (state != WalterQuestState.Active || amount <= 0) return;
            earned += Math.Min(amount, Target - earned);
            if (earned == Target) state = WalterQuestState.ReadyToClaim;
        }

        internal void CloseDay(int day)
        {
            if (state == WalterQuestState.Active && (long)day - acceptedDay + 1 >= Days)
                state = WalterQuestState.Failed;
        }

        internal bool IsValid(int day)
        {
            if (state == WalterQuestState.Available) return acceptedDay == 0 && earned == 0;
            if (acceptedDay < 1 || acceptedDay > day || earned < 0) return false;
            return state switch
            {
                WalterQuestState.Active => earned < Target && (long)day - acceptedDay < Days,
                WalterQuestState.Failed => earned < Target && (long)day - acceptedDay >= Days,
                WalterQuestState.ReadyToClaim or WalterQuestState.Claimed => earned == Target,
                _ => false
            };
        }
    }
}
