using System;
using UnityEngine;

/// <summary>
/// Picks up <see cref="Collectible"/>s the plane touches and reports each one through <see cref="Collected"/>.
/// It does not know about score; the score system listens to the event. A crashed plane picks up nothing.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlaneCrash))]
public class PlaneCollector : MonoBehaviour
{
    public event Action<Collectible> Collected;

    private PlaneCrash crash;

    private void Awake()
    {
        crash = GetComponent<PlaneCrash>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (crash.HasCrashed || !other.TryGetComponent<Collectible>(out Collectible collectible))
        {
            return;
        }

        if (collectible.Collect())
        {
            Collected?.Invoke(collectible);
        }
    }
}
