using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the thrust button from keyboard, mouse/touch/pen (via the generic pointer) and gamepad.
/// </summary>
public class PlaneInput : MonoBehaviour, IFlightInput
{
    private InputAction thrustAction;

    public bool IsHolding => thrustAction.IsPressed();

    public bool PressedThisFrame => thrustAction.WasPressedThisFrame();

    private void Awake()
    {
        thrustAction = new InputAction("Thrust", InputActionType.Button);
        thrustAction.AddBinding("<Keyboard>/space");
        thrustAction.AddBinding("<Pointer>/press");
        thrustAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        thrustAction.Enable();
    }

    private void OnDisable()
    {
        thrustAction.Disable();
    }

    private void OnDestroy()
    {
        thrustAction.Dispose();
    }
}
