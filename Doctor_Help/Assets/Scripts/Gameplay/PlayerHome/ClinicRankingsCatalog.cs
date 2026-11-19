using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(menuName = "Doctor Help/Data/Clinic Rankings")]
    public sealed class ClinicRankingsCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class Rival
        {
            public string Name;
            public string Id;
            [Range(0f, 5f)] public float StartingRating;
            public Sprite Logo;
            [Min(0)] public int StartingGold;
            [Min(0)] public int DailyGrowth;
        }

        [SerializeField] private string playerClinicName;
        [SerializeField] private Sprite playerClinicLogo;
        [SerializeField] private Rival[] rivals = Array.Empty<Rival>();

        internal readonly struct Standing
        {
            public readonly string Name;
            public readonly Sprite Logo;
            public readonly long Gold;
            public readonly bool IsPlayer;
            public readonly float Rating;

            public Standing(string name, Sprite logo, long gold, float rating, bool isPlayer = false)
            {
                Name = name;
                Logo = logo;
                Gold = gold;
                Rating = rating;
                IsPlayer = isPlayer;
            }
        }

        internal float GetClinicRating(string clinicId, PlayerProfileService profile)
        {
            foreach (var rival in rivals)
                if (StringComparer.Ordinal.Equals(rival.Id, clinicId))
                    return ResolveRivalRating(rival, profile);
            return profile != null && profile.IsLoaded ? profile.Rating : PlayerProfileData.DefaultRating;
        }

        private static float ResolveRivalRating(Rival rival, PlayerProfileService profile) =>
            profile != null && profile.IsLoaded ? profile.GetRivalRating(rival.Id, rival.StartingRating) : rival.StartingRating;

        internal bool IsPlayerFirst(int day, long gold)
        {
            foreach (var rival in rivals)
                if (CompareRank(gold, playerClinicName, GoldOnDay(rival, day), rival.Name) >= 0)
                    return false;
            return true;
        }

        private static long GoldOnDay(Rival rival, int day) =>
            rival.StartingGold + Math.Max(0L, (long)day - 1) * rival.DailyGrowth;

        private static int CompareRank(long leftGold, string leftName, long rightGold, string rightName)
        {
            int byGold = rightGold.CompareTo(leftGold);
            return byGold != 0 ? byGold : StringComparer.Ordinal.Compare(leftName, rightName);
        }

        internal List<Standing> GetStandings(PlayerProfileService profile)
        {
            var standings = new List<Standing> { new Standing(playerClinicName, playerClinicLogo, profile.Money, profile.Rating, true) };
            foreach (var rival in rivals)
                standings.Add(new Standing(rival.Name, rival.Logo, GoldOnDay(rival, profile.CurrentDay),
                    ResolveRivalRating(rival, profile)));
            standings.Sort((left, right) => CompareRank(left.Gold, left.Name, right.Gold, right.Name));
            return standings;
        }
    }
}
