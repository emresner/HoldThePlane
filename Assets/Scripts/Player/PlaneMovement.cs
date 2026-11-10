using UnityEngine;

/// <summary>
/// Vertical flight: accelerates towards a rise speed while the input is held and towards a fall speed otherwise.
/// The plane never moves horizontally; the world scrolls past it.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlaneMovement : MonoBehaviour
{
    [Header("Speed (units/s)")]
    [SerializeField] private float maxRiseSpeed = 3f;
    [SerializeField] private float maxFallSpeed = 3.5f;

    [Header("Acceleration (units/s²)")]
    [SerializeField] private float riseAcceleration = 14f;
    [SerializeField] private float fallAcceleration = 12f;

    [Header("Vertical bounds (world Y)")]
    [SerializeField] private float minY = -2f;
    [SerializeField] private float maxY = 2f;

    [Header("Live values (read only)")]
    [SerializeField, DebugReadOnly] private float currentVerticalSpeed;

    private Rigidbody2D body;
    private IFlightInput flightInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        flightInput = GetComponent<IFlightInput>();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool holding = flightInput.IsHolding;

        float targetSpeed = holding ? maxRiseSpeed : -maxFallSpeed;
        float acceleration = holding ? riseAcceleration : fallAcceleration;
        float speedY = Mathf.MoveTowards(body.linearVelocity.y, targetSpeed, acceleration * dt);

        // Limit the velocity so this physics step cannot carry the plane past the bounds.
        float y = body.position.y;
        speedY = Mathf.Clamp(speedY, (minY - y) / dt, (maxY - y) / dt);

        body.linearVelocity = new Vector2(0f, speedY);
        currentVerticalSpeed = speedY;
    }
}
