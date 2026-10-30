using UnityEngine;
using DoctorHelp;

/// <summary>
/// Feeds pointer hold state and screen UV into the cloth see-through material,
/// and moves the magnifier object along with the pointer while holding.
/// Attach to the NPC cloth renderer that uses M_Cloth_1 / S_Cloth_Seethrough.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ClothSeeThroughController : MonoBehaviour
{
    static readonly int HoldId = Shader.PropertyToID("_Hold");
    static readonly int CutoutPositionId = Shader.PropertyToID("_Cutout_Position");

    [Tooltip("Pointer magnifier, resolved from the scene examination selection service.")]
    [SerializeField] Transform magnifier;

    [Tooltip("World-space offset from the pointer, used when the sprite pivot is not the lens center.")]
    [SerializeField] Vector2 magnifierOffset;

    [Tooltip("Camera rendering the scene. Falls back to Camera.main.")]
    [SerializeField] Camera targetCamera;

    ExaminationToolSelection _selection;
    Renderer _renderer;
    MaterialPropertyBlock _propertyBlock;
    bool _isHolding;
    float _magnifierWorldZ;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        ResolveReferences();
        ApplyHold(false);
        SetMagnifierActive(false);
    }

    void Update()
    {
        if (_selection != null && _selection.IsUsingXray)
        {
            Vector2 screenPos = _selection.ScreenPosition;
            float w = Screen.width;
            float h = Screen.height;
            if (w <= 0f || h <= 0f)
                return;

            Vector2 screenUv = new Vector2(screenPos.x / w, screenPos.y / h);
            Apply(true, screenUv);
            MoveMagnifier(screenPos);
            SetMagnifierActive(true);
            _isHolding = true;
        }
        else if (_isHolding)
        {
            ApplyHold(false);
            SetMagnifierActive(false);
            _isHolding = false;
        }
    }

    void OnDisable()
    {
        if (_renderer == null)
            return;

        ApplyHold(false);
        SetMagnifierActive(false);
        _isHolding = false;
    }

    void ResolveReferences()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        var selections = FindObjectsByType<ExaminationToolSelection>(FindObjectsSortMode.None);
        foreach (var selection in selections)
        {
            if (selection.gameObject.scene != gameObject.scene) continue;
            _selection = selection;
            magnifier = selection.Magnifier;
            break;
        }
        if (magnifier != null) _magnifierWorldZ = magnifier.position.z;
    }
    void MoveMagnifier(Vector2 screenPos)
    {
        if (magnifier == null || targetCamera == null)
            return;

        float depth = _magnifierWorldZ - targetCamera.transform.position.z;
        Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        magnifier.position = new Vector3(
            world.x + magnifierOffset.x,
            world.y + magnifierOffset.y,
            _magnifierWorldZ);
    }

    void SetMagnifierActive(bool active)
    {
        if (magnifier != null && magnifier.gameObject.activeSelf != active)
            magnifier.gameObject.SetActive(active);
    }

    void Apply(bool hold, Vector2 screenUv)
    {
        // Managed helper objects are recreated after a Play Mode script reload.
        _propertyBlock ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(HoldId, hold ? 1f : 0f);
        _propertyBlock.SetVector(CutoutPositionId, new Vector4(screenUv.x, screenUv.y, 0f, 0f));
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    void ApplyHold(bool hold)
    {
        _propertyBlock ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(HoldId, hold ? 1f : 0f);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}



