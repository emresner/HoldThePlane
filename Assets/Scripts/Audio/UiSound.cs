using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Plays a sound when the player uses this UI element. Put it on a Button (default: on click), on the pause button (on press) or on a slider (on release).
/// The button's own behaviour does not know about it.
/// </summary>
public class UiSound : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Trigger
    {
        Click,
        PointerDown,
        PointerUp
    }

    [SerializeField] private SoundId sound = SoundId.ButtonMain;
    [Tooltip("Click = the Button was clicked. PointerDown = the moment the press starts (for things that must react at once). PointerUp = when the press ends (for a slider).")]
    [SerializeField] private Trigger trigger = Trigger.Click;

    /// <summary>Raised right after the sound was requested.</summary>
    public event Action Played;

    private Button button;

    private void OnEnable()
    {
        if (trigger == Trigger.Click && TryGetComponent(out button))
        {
            button.onClick.AddListener(Play);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Play);
            button = null;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (trigger == Trigger.PointerDown)
        {
            Play();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (trigger == Trigger.PointerUp)
        {
            Play();
        }
    }

    private void Play()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(sound);
        }

        Played?.Invoke();
    }
}
