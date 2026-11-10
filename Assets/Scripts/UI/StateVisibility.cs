using System;
using UnityEngine;

/// <summary>
/// Shows <see cref="target"/> only while the game is in one of the listed states (for example the menu panel only in Menu).
/// Put it on an object that stays active: a switched-off object cannot listen, so the thing it switches is a different (child) object.
/// </summary>
public class StateVisibility : MonoBehaviour
{
    [SerializeField] private RunController runController;
    [SerializeField] private GameObject target;
    [SerializeField] private GameState[] visibleIn;

    private void Awake()
    {
        // Hidden until the first state arrives, so nothing flashes on the first frame.
        target.SetActive(false);
    }

    private void OnEnable()
    {
        runController.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        runController.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        target.SetActive(Array.IndexOf(visibleIn, state) >= 0);
    }
}
