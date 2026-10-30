using System;
using UnityEngine;
namespace DoctorHelp
{
    public sealed class ThermometerDisplay : MonoBehaviour
    {
        public const float MinimumCelsius=33;
        public const float MaximumCelsius=43;
        private static readonly int MercuryTop=Shader.PropertyToID("_MercuryTop");
        [SerializeField] private SpriteRenderer mercury;
        [SerializeField] private Transform minimumMark;
        [SerializeField] private Transform maximumMark;
        private MaterialPropertyBlock properties;
        private float DisplayedCelsius {get;set;}=MinimumCelsius;
        public bool IsConfigured=>mercury!=null && minimumMark!=null && maximumMark!=null &&
            mercury.sharedMaterial!=null && mercury.sharedMaterial.HasProperty(MercuryTop) &&
            mercury.transform.InverseTransformPoint(maximumMark.position).y>mercury.transform.InverseTransformPoint(minimumMark.position).y;
        public void SetTemperature(float celsius)
        {
            if(!IsConfigured)throw new InvalidOperationException("Thermometer requires mercury material and ascending Min (33 C) / Max (43 C) marks.");
            DisplayedCelsius=Mathf.Clamp(celsius,MinimumCelsius,MaximumCelsius);
            float fraction=Mathf.InverseLerp(MinimumCelsius,MaximumCelsius,DisplayedCelsius);
            var top=Vector3.Lerp(minimumMark.position,maximumMark.position,fraction);
            properties ??= new MaterialPropertyBlock();
            mercury.GetPropertyBlock(properties);
            properties.SetFloat(MercuryTop,mercury.transform.InverseTransformPoint(top).y);
            mercury.SetPropertyBlock(properties);
        }
    }
}


