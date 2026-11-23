using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // Small editor-baked alpha samples, so mobile textures remain compressed/non-readable.
    // Mirrors S_Bone_Crack's UV*2 + _Crack_Offset and repeating crack texture.
    public sealed class BoneClueMask : ScriptableObject
    {
        [SerializeField] private Sprite source;
        [SerializeField] private int width = 64, height = 128, crackWidth = 128, crackHeight = 128;
        [SerializeField] private byte[] boneAlpha, crackAlpha;
        public void FindCracks(SpriteRenderer renderer, Vector2 offset, List<Vector2> points)
        {
            points.Clear();
            if (renderer == null || renderer.sprite != source || boneAlpha == null || crackAlpha == null) return;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                if (boneAlpha[y*width+x] < 110) continue;
                var uv = new Vector2((x+.5f)/width, (y+.5f)/height);
                int cx = Mathf.Min(crackWidth-1, (int)(Mathf.Repeat(uv.x*2+offset.x,1)*crackWidth));
                int cy = Mathf.Min(crackHeight-1, (int)(Mathf.Repeat(uv.y*2+offset.y,1)*crackHeight));
                if (crackAlpha[cy*crackWidth+cx] > 90) points.Add(uv);
            }
        }
        public static Vector3 WorldPoint(SpriteRenderer r, Vector2 uv)
        {
            var sprite = r.sprite; Vector2 size = r.drawMode == SpriteDrawMode.Simple ? (Vector2)sprite.bounds.size : r.size;
            Vector2 pivot = sprite.pivot / sprite.rect.size;
            if (r.flipX) uv.x = 1-uv.x; if (r.flipY) uv.y = 1-uv.y;
            return r.transform.TransformPoint((uv.x-pivot.x)*size.x,(uv.y-pivot.y)*size.y,0);
        }
    }
}
