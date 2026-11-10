using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The on-screen pause button: put it on a (usually invisible) Image that is bigger than the drawn button, so a thumb does not have to be exact.
/// It reacts when the press starts, not when it is released: the plane would otherwise keep rising for as long as the finger is on the button.
/// </summary>
public class PauseButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private RunController runController;

    public void OnPointerDown(PointerEventData eventData)
    {
        runController.Pause();
    }
}
