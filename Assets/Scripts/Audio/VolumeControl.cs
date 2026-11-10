using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The sound controls of the menu and the pause panel: a speaker button (sound on/off) and a volume slider.
/// It talks to the <see cref="AudioManager"/> and keeps the picture in sync with it; the same prefab is used wherever it is needed.
/// </summary>
public class VolumeControl : MonoBehaviour
{
    [SerializeField] private Button muteButton;
    [SerializeField] private SpeakerIcon speakerIcon;
    [SerializeField] private Slider slider;
    [Tooltip("The sound component on the slider; its release is the moment the chosen volume is saved and heard once.")]
    [SerializeField] private UiSound sliderSound;
    [Tooltip("Volume that comes back when sound is switched on while the slider is at zero.")]
    [SerializeField, Range(0.05f, 1f)] private float defaultVolume = 0.5f;

    private AudioManager audioManager;

    private void OnEnable()
    {
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        muteButton.onClick.AddListener(ToggleMute);
        slider.onValueChanged.AddListener(OnSliderChanged);
        sliderSound.Played += SaveSettings;
        audioManager.SettingsChanged += Refresh;
        slider.SetValueWithoutNotify(audioManager.Volume);
        speakerIcon.SetOn(!audioManager.IsSilent, false);
    }

    private void OnDisable()
    {
        if (audioManager == null)
        {
            return;
        }

        muteButton.onClick.RemoveListener(ToggleMute);
        slider.onValueChanged.RemoveListener(OnSliderChanged);
        sliderSound.Played -= SaveSettings;
        audioManager.SettingsChanged -= Refresh;

        // Closing the panel while the slider was moved with the keyboard (no release) must not lose the change.
        audioManager.SaveSettings();
        audioManager = null;
    }

    private void ToggleMute()
    {
        if (audioManager.IsSilent)
        {
            if (audioManager.Volume <= 0.001f)
            {
                audioManager.SetVolume(defaultVolume);
            }

            audioManager.SetMuted(false);
            audioManager.Play(SoundId.ButtonLight); // the confirmation that sound is back
        }
        else
        {
            audioManager.SetMuted(true);
        }
    }

    private void OnSliderChanged(float value)
    {
        // Moving the slider while muted means "I want to hear it".
        if (audioManager.Muted && value > 0f)
        {
            audioManager.SetMuted(false);
        }

        audioManager.SetVolume(value);
    }

    private void SaveSettings()
    {
        audioManager.SaveSettings();
    }

    private void Refresh()
    {
        slider.SetValueWithoutNotify(audioManager.Volume);
        speakerIcon.SetOn(!audioManager.IsSilent, true);
    }
}
