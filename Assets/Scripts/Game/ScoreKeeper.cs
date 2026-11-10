using System;
using UnityEngine;

/// <summary>
/// Keeps the score of one run: points for the distance flown plus the value of every collected pickup.
/// It only holds the number; whatever shows it (the HUD) listens to <see cref="ScoreChanged"/>.
/// Distance comes straight from <see cref="WorldScroller"/>, so a faster world earns points faster, and the score freezes
/// by itself when the world stops after a crash.
/// </summary>
public class ScoreKeeper : MonoBehaviour
{
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private PlaneCollector collector;
    [Tooltip("Points for every world unit flown. 1 means one point per metre; 10 gives bigger, more arcade-like numbers.")]
    [SerializeField, Min(0f)] private float pointsPerUnit = 1f;

    /// <summary>Raised with the new total whenever the score changes.</summary>
    public event Action<int> ScoreChanged;

    /// <summary>Raised with the points of a pickup the moment it is collected (the HUD uses it for a little highlight).</summary>
    public event Action<int> BonusEarned;

    // Greyed out in the Inspector: watch them while playing.
    [field: SerializeField, DebugReadOnly] public int Score { get; private set; }
    [field: SerializeField, DebugReadOnly] public int DistancePoints { get; private set; }
    [field: SerializeField, DebugReadOnly] public int BonusPoints { get; private set; }
    [field: SerializeField, DebugReadOnly] public int StarsCollected { get; private set; }

    private void Awake()
    {
        // Live values are saved with the scene, so start from zero explicitly.
        Score = 0;
        DistancePoints = 0;
        BonusPoints = 0;
        StarsCollected = 0;
    }

    private void OnEnable()
    {
        collector.Collected += HandleCollected;
    }

    private void OnDisable()
    {
        collector.Collected -= HandleCollected;
    }

    private void Update()
    {
        DistancePoints = Mathf.FloorToInt(worldScroller.DistanceTravelled * pointsPerUnit);
        Refresh();
    }

    private void HandleCollected(Collectible collectible)
    {
        BonusPoints += collectible.Points;
        StarsCollected++;
        Refresh();
        BonusEarned?.Invoke(collectible.Points);
    }

    private void Refresh()
    {
        int total = DistancePoints + BonusPoints;
        if (total == Score)
        {
            return;
        }

        Score = total;
        ScoreChanged?.Invoke(Score);
    }
}
