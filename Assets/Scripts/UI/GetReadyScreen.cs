using UnityEngine;

/// <summary>
/// The Get Ready screen explains the controls only when the player came from the menu; after Replay it is just the "GET READY" title.
/// (Whether the screen is visible at all is decided by a <see cref="StateVisibility"/> next to it.)
/// </summary>
public class GetReadyScreen : MonoBehaviour
{
    [Tooltip("The hand and the hint text, shown only on the first run after the menu.")]
    [SerializeField] private GameObject controlsHint;

    private void Awake()
    {
        controlsHint.SetActive(!GameSession.IsReplay);
    }
}
