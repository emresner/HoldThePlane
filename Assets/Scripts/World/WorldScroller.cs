using UnityEngine;

/// <summary>
/// Single source of truth for how fast the world moves and how far the player has travelled.
/// Scrolling layers, score, difficulty and level generation all read from here.
/// The world starts at <c>baseSpeed</c> and speeds up by a fixed percentage every interval (compounding) until it reaches <c>maxSpeed</c>.
/// </summary>
[DefaultExecutionOrder(-200)] // updates first, so everything else sees this frame's distance (props must line up with the scrolling ground)
public class WorldScroller : MonoBehaviour
{
    [Header("Speed (units/s)")]
    [Tooltip("World speed at the start of a run.")]
    [SerializeField] private float baseSpeed = 5f;
    [Tooltip("The world never gets faster than this.")]
    [SerializeField] private float maxSpeed = 8f;

    [Header("Testing")]
    [Tooltip("Starts the run as if this many seconds had already passed (speed and difficulty included). Keep at 0 for normal play.")]
    [SerializeField, Min(0f)] private float startElapsedTime;

    [Header("Speed ramp")]
    [Tooltip("How much faster the world gets every interval, in percent of the current speed.")]
    [SerializeField, Min(0f)] private float gainPercent = 1.5f;
    [Tooltip("Seconds between two speed-ups.")]
    [SerializeField, Min(0.1f)] private float gainInterval = 4f;

    // The four values below are greyed out in the Inspector: they are there to be watched while playing, not edited.

    /// <summary>Current world speed in units per second.</summary>
    [field: SerializeField, DebugReadOnly] public float Speed { get; private set; }

    public float MaxSpeed => maxSpeed;

    /// <summary>Seconds the world has been running in this run.</summary>
    [field: SerializeField, DebugReadOnly] public float ElapsedTime { get; private set; }

    /// <summary>Total distance scrolled so far, in world units.</summary>
    [field: SerializeField, DebugReadOnly] public float DistanceTravelled { get; private set; }

    [field: SerializeField, DebugReadOnly] public bool IsRunning { get; private set; }

    /// <summary>Starts the world moving. It stays frozen (distance 0) until <c>RunController</c> calls this.</summary>
    public void Begin()
    {
        IsRunning = true;
    }

    /// <summary>Freezes the world (and everything positioned from <see cref="DistanceTravelled"/>). A scene reload resets it.</summary>
    public void Stop()
    {
        IsRunning = false;
    }

    private void Awake()
    {
        // Reset the live values explicitly: the Inspector keeps whatever was last saved with the scene.
        ElapsedTime = startElapsedTime;
        Speed = SpeedAt(ElapsedTime);
        DistanceTravelled = 0f;
        IsRunning = false;
    }

    /// <summary>World speed after the given number of seconds (before the cap it grows by the same percentage every interval).</summary>
    public float SpeedAt(float seconds)
    {
        return Mathf.Min(maxSpeed, baseSpeed * Mathf.Pow(1f + gainPercent / 100f, seconds / gainInterval));
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        ElapsedTime += Time.deltaTime;
        Speed = SpeedAt(ElapsedTime);
        DistanceTravelled += Speed * Time.deltaTime;
    }

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(baseSpeed, maxSpeed);
    }
}
