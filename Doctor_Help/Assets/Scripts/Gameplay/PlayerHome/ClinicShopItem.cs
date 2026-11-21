using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class ClinicShopItem : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0)] private int price;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite poster;
        [SerializeField] private Vector2 posterOffset;
        [SerializeField] private string furnitureSlot;
        [SerializeField] private bool defaultFurniture;
        [SerializeField, Range(0, ClinicDemand.MaxFurnitureLevel)] private int furnitureLevel;
        [SerializeField] private GameObject equippedVisual;
        [SerializeField] private Transform movingPart;

        internal string Id => itemId;
        internal string DisplayName => displayName;
        internal string Description => description;
        internal int Price => price;
        internal Sprite Icon => icon;
        internal Sprite Poster => poster;
        internal Vector2 PosterOffset => posterOffset;
        internal string FurnitureSlot => furnitureSlot;
        internal bool IsEquippable => !string.IsNullOrWhiteSpace(furnitureSlot);
        internal bool IsFurniture => IsEquippable && furnitureSlot != ClinicDemand.ReceptionistSlot;
        internal GameObject EquippedVisual => equippedVisual != null ? equippedVisual : gameObject;
        internal Transform MovingPart => movingPart;
        internal bool IsDefaultFurniture => defaultFurniture;
        internal int FurnitureLevel => Mathf.Clamp(furnitureLevel, 0, ClinicDemand.MaxFurnitureLevel);
        // Electricity used per day while equipped, paid at the start of the next day: 5, plus 2.5 per upgrade level.
        internal const float BaseElectricityPerDay = 5f;
        internal const float ElectricityPerLevel = 2.5f;
        internal float ElectricityPerDay => BaseElectricityPerDay + ElectricityPerLevel * FurnitureLevel;
        internal bool IsConfigured => !string.IsNullOrWhiteSpace(itemId) &&
            !string.IsNullOrWhiteSpace(displayName) && price >= 0 && (!defaultFurniture || IsEquippable);
    }
}
