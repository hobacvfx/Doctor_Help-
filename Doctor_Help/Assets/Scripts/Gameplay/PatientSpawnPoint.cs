using System;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PatientSpawnPoint : MonoBehaviour
    {
        private GameObject patient;
        public void EnsurePatient(NpcCatalog catalog)
        {
            if (patient != null) return;
            if (catalog == null || catalog.prefabs == null || catalog.prefabs.Length == 0 ||
                Array.Exists(catalog.prefabs, prefab => prefab == null))
                throw new InvalidOperationException("NPC catalog is missing or contains invalid prefab references.");
            var prefab = catalog.prefabs[UnityEngine.Random.Range(0, catalog.prefabs.Length)];
            patient = Instantiate(prefab, transform, false);
            patient.transform.localPosition = Vector3.zero;
            patient.transform.localRotation = Quaternion.identity;
            patient.name = prefab.name;
        }
    }
}
