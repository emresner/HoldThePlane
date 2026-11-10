using UnityEngine;

/// <summary>
/// Keeps the vertical view height fixed and zooms out on narrow screens so the visible width never drops below a minimum.
/// Wide screens (16:9, 20:9, ...) keep the reference height untouched.
/// Only what is SEEN changes: the playfield (strips, obstacles, plane limits) is fixed in world units and sized for the reference height,
/// and the extra screen on narrow shapes is filled by the ground and ceiling fill blocks, so every screen plays exactly the same.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-100)] // runs before the things that read the camera size (scrolling layers, edge anchors)
public class CameraFit : MonoBehaviour
{
    [SerializeField] private float referenceHeight = 7f;
    [SerializeField] private float minimumWidth = 12f;

    /// <summary>Height (world units) the screen shows on wide screens; the playfield is designed for this.</summary>
    public float ReferenceHeight => referenceHeight;

    private Camera cam;
    private float lastAspect;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        Apply(); // right away, so pieces created in the first frame already see the final size
    }

    private void LateUpdate()
    {
        if (Mathf.Approximately(cam.aspect, lastAspect))
        {
            return;
        }

        Apply();
    }

    private void Apply()
    {
        lastAspect = cam.aspect;
        float sizeForHeight = referenceHeight * 0.5f;
        float sizeForWidth = minimumWidth / (2f * cam.aspect);
        cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
    }

    private void OnValidate()
    {
        referenceHeight = Mathf.Max(0.1f, referenceHeight);
        minimumWidth = Mathf.Max(0.1f, minimumWidth);
        lastAspect = 0f;
    }
}
