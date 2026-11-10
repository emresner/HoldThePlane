using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The pause menu: Resume, Restart and Menu buttons. It only calls back into the <see cref="RunController"/>.
/// (Whether the menu is visible is decided by a <see cref="StateVisibility"/> next to it.)
/// </summary>
public class PauseScreen : MonoBehaviour
{
    [SerializeField] private RunController runController;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private void OnEnable()
    {
        resumeButton.onClick.AddListener(runController.Resume);
        restartButton.onClick.AddListener(runController.Replay);
        menuButton.onClick.AddListener(runController.ReturnToMenu);
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveListener(runController.Resume);
        restartButton.onClick.RemoveListener(runController.Replay);
        menuButton.onClick.RemoveListener(runController.ReturnToMenu);
    }
}
