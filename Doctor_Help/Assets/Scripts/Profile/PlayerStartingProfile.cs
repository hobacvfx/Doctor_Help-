using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(menuName = "Doctor Help/Data/Player Starting Profile")]
    public sealed class PlayerStartingProfile : ScriptableObject
    {
        [SerializeField] private int money = 500;
        [SerializeField] private PillProfile pills;
        [SerializeField] private ClinicRankingsCatalog clinicRankings;
        public PillProfile Pills => pills;
        internal ClinicRankingsCatalog ClinicRankings => clinicRankings;
        public PlayerProfileData CreateProfile() => new PlayerProfileData { money = money };
    }
}
