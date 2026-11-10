using System;
using UnityEngine;

/// <summary>
/// Detects the plane touching a <see cref="Hazard"/>, freezes the plane and reports the crash.
/// What happens to the rest of the game is up to whoever listens to <see cref="Crashed"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlaneMovement))]
public class PlaneCrash : MonoBehaviour
{
    public event Action Crashed;

    [field: SerializeField, DebugReadOnly] public bool HasCrashed { get; private set; }

    private Rigidbody2D body;
    private PlaneMovement movement;
    private Animator propellerAnimator;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlaneMovement>();
        propellerAnimator = GetComponentInChildren<Animator>();
        HasCrashed = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (HasCrashed || !other.TryGetComponent<Hazard>(out _))
        {
            return;
        }

        HasCrashed = true;
        movement.enabled = false;
        body.linearVelocity = Vector2.zero;
        propellerAnimator.speed = 0f;
        Crashed?.Invoke();
    }
}
