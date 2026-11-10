using UnityEngine;

/// <summary>
/// Visual-only pitch that follows the vertical speed. Lives on the sprite child so the collider never rotates.
/// </summary>
public class PlaneTilt : MonoBehaviour
{
    [SerializeField] private float maxTiltAngle = 20f;
    [SerializeField] private float speedForMaxTilt = 3f;
    [SerializeField] private float smoothing = 10f;

    private Rigidbody2D body;
    private float currentAngle;

    private void Awake()
    {
        body = GetComponentInParent<Rigidbody2D>();
    }

    private void Update()
    {
        float tilt = Mathf.Clamp(body.linearVelocity.y / speedForMaxTilt, -1f, 1f);
        float targetAngle = tilt * maxTiltAngle;

        currentAngle = Mathf.Lerp(currentAngle, targetAngle, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}
