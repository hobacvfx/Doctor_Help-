using UnityEngine;
using UnityEngine.InputSystem;

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

    [Tooltip("Magnifier object (KinhLup). Shown only while Hold is on.")]
    [SerializeField] Transform magnifier;

    [Tooltip("World-space offset from the pointer, used when the sprite pivot is not the lens center.")]
    [SerializeField] Vector2 magnifierOffset;

    [Tooltip("Camera rendering the scene. Falls back to Camera.main.")]
    [SerializeField] Camera targetCamera;

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
        if (TryGetPressedScreenPosition(out Vector2 screenPos))
        {
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

    /// <summary>
    /// Touch on mobile, left mouse on PC. Prefers an active touch when both exist.
    /// </summary>
    static bool TryGetPressedScreenPosition(out Vector2 screenPosition)
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    void ResolveReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (magnifier == null)
            magnifier = FindMagnifierInScene();

        if (magnifier != null)
            _magnifierWorldZ = magnifier.position.z;
    }

    static Transform FindMagnifierInScene()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == "KinhLup")
                return candidate;
        }

        return null;
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
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(HoldId, hold ? 1f : 0f);
        _propertyBlock.SetVector(CutoutPositionId, new Vector4(screenUv.x, screenUv.y, 0f, 0f));
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    void ApplyHold(bool hold)
    {
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(HoldId, hold ? 1f : 0f);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}
