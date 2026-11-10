using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns the pause keys (Esc, P, gamepad Start) into <see cref="RunController.TogglePause"/>.
/// On Android the system back button arrives as Esc, so it pauses too.
/// Lives next to the <see cref="RunController"/>; the on-screen pause button is a separate script.
/// </summary>
[RequireComponent(typeof(RunController))]
public class PauseInput : MonoBehaviour
{
    private RunController runController;
    private InputAction pauseAction;

    private void Awake()
    {
        runController = GetComponent<RunController>();
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Keyboard>/p");
        pauseAction.AddBinding("<Gamepad>/start");
    }

    private void OnEnable()
    {
        pauseAction.performed += HandlePerformed;
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        pauseAction.Disable();
        pauseAction.performed -= HandlePerformed;
    }

    private void OnDestroy()
    {
        pauseAction.Dispose();
    }

    private void HandlePerformed(InputAction.CallbackContext context)
    {
        runController.TogglePause();
    }
}
