using UnityEngine;

namespace DoctorHelp
{
    internal static class ExaminationPuff
    {
        internal static void Play(ParticleSystem prefab, GameObject view, Vector3 worldPosition)
        {
            if (prefab == null || view == null) return;

            // Keep the puff outside the view so it can finish when the instrument is hidden.
            var puff = Object.Instantiate(prefab, worldPosition, prefab.transform.rotation, view.transform.parent);
            if (puff.TryGetComponent<ParticleSystemRenderer>(out var renderer))
            {
                int layer = renderer.sortingLayerID;
                int order = renderer.sortingOrder;
                foreach (var surface in view.GetComponentsInChildren<Renderer>(true))
                {
                    int comparison = SortingLayer.GetLayerValueFromID(surface.sortingLayerID)
                        .CompareTo(SortingLayer.GetLayerValueFromID(layer));
                    if (comparison < 0) continue;
                    if (comparison > 0)
                    {
                        layer = surface.sortingLayerID;
                        order = surface.sortingOrder + 1;
                    }
                    else order = Mathf.Max(order, surface.sortingOrder + 1);
                }
                renderer.sortingLayerID = layer;
                renderer.sortingOrder = order;
            }
            var settings = puff.main;
            settings.stopAction = ParticleSystemStopAction.Destroy;
            puff.Play();
        }
    }
}
