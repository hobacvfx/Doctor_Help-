using UnityEngine;

namespace DoctorHelp
{
    // Takes a GPU snapshot of a duplicate renderer, preserving fracture shader/property-block state.
    // No changes to the real NPC, shared material, source alpha, pose or source visibility.
    public sealed class ClueSpriteCapture : MonoBehaviour
    {
        [SerializeField] private Camera captureCamera;
        public RenderTexture Capture(SpriteRenderer source)
        {
            if (captureCamera == null || source == null || source.sprite == null) return null;
            var copy = new GameObject("Clue sprite snapshot"); copy.layer = 31;
            var rt = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
            var previousTarget = captureCamera.targetTexture;
            try
            {
                var r = copy.AddComponent<SpriteRenderer>(); r.sprite = source.sprite; r.sharedMaterial = source.sharedMaterial; r.color = source.color;
                r.flipX = source.flipX; r.flipY = source.flipY; r.drawMode = source.drawMode; r.size = source.size;
                var properties = new MaterialPropertyBlock(); source.GetPropertyBlock(properties); r.SetPropertyBlock(properties);
                copy.transform.SetPositionAndRotation(source.transform.position - source.bounds.center, source.transform.rotation); copy.transform.localScale = source.transform.lossyScale;
                captureCamera.transform.SetPositionAndRotation(new Vector3(0,0,-10), Quaternion.identity);
                captureCamera.orthographic = true; captureCamera.orthographicSize = Mathf.Max(r.bounds.size.x, r.bounds.size.y) * .56f;
                captureCamera.aspect = 1; captureCamera.targetTexture = rt; captureCamera.Render(); captureCamera.targetTexture = null;
                return rt;
            }
            catch { RenderTexture.ReleaseTemporary(rt); throw; }
            finally
            {
                captureCamera.targetTexture = previousTarget;
                copy.SetActive(false);
                Destroy(copy);
            }
        }
    }
}
