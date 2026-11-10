using UnityEngine;

/// <summary>
/// Moves a spawned object left with the world and destroys it once it has left the screen.
/// The position is computed from <see cref="WorldScroller.DistanceTravelled"/> (like <see cref="ScrollingLayer"/>),
/// so stopping the scroller freezes the object and there is no drift.
/// The object's origin must be its left edge.
/// </summary>
public class ScrollWithWorld : MonoBehaviour
{
    [Tooltip("How far past the left screen edge the object is destroyed (world units).")]
    [SerializeField] private float despawnMargin = 2f;

    private WorldScroller worldScroller;
    private Camera cam;
    private float originX;
    private float originDistance;
    private float width;

    /// <summary>
    /// Call right after Instantiate. The object's left edge sits at <paramref name="startX"/> when the world has travelled
    /// <paramref name="atDistance"/>; <paramref name="objectWidth"/> decides when its right edge has left the screen.
    /// </summary>
    public void Initialize(WorldScroller scroller, float startX, float atDistance, float objectWidth)
    {
        worldScroller = scroller;
        originX = startX;
        originDistance = atDistance;
        width = objectWidth;
        cam = Camera.main;
        ApplyPosition();
    }

    private void Update()
    {
        ApplyPosition();

        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        if (transform.position.x + width < camLeft - despawnMargin)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyPosition()
    {
        Vector3 position = transform.position;
        position.x = originX - (worldScroller.DistanceTravelled - originDistance);
        transform.position = position;
    }
}
