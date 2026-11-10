using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The plane picker in the main menu. The arrow buttons (mouse and touch), Left/Right or A/D, and the gamepad d-pad
/// show the previous or next plane colour. The old plane slides out and fades, the new one slides in from the other side.
/// The choice is saved, and the game scene reads it (see <see cref="PlaneSkinApplier"/>).
/// </summary>
public class PlaneSelector : MonoBehaviour
{
    [SerializeField] private PlaneSkinCatalog catalog;
    [Tooltip("The preview's Animator; its controller is swapped for the chosen colour.")]
    [SerializeField] private Animator previewAnimator;
    [Tooltip("The picture that slides and fades (a child of the preview, so the Animator's bobbing is not disturbed).")]
    [SerializeField] private RectTransform visual;
    [SerializeField] private Image visualImage;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [Tooltip("How far (in canvas units) the plane slides while it leaves and enters.")]
    [SerializeField] private float slideDistance = 160f;
    [SerializeField] private float leaveSeconds = 0.12f;
    [SerializeField] private float enterSeconds = 0.18f;

    private InputAction previousAction;
    private InputAction nextAction;
    private int index;
    private bool isChanging;

    private void Awake()
    {
        previousAction = new InputAction("PreviousPlane", InputActionType.Button);
        previousAction.AddBinding("<Keyboard>/leftArrow");
        previousAction.AddBinding("<Keyboard>/a");
        previousAction.AddBinding("<Gamepad>/dpad/left");

        nextAction = new InputAction("NextPlane", InputActionType.Button);
        nextAction.AddBinding("<Keyboard>/rightArrow");
        nextAction.AddBinding("<Keyboard>/d");
        nextAction.AddBinding("<Gamepad>/dpad/right");
    }

    private void OnEnable()
    {
        previousButton.onClick.AddListener(ShowPrevious);
        nextButton.onClick.AddListener(ShowNext);
        previousAction.performed += OnPreviousPerformed;
        nextAction.performed += OnNextPerformed;
        previousAction.Enable();
        nextAction.Enable();
    }

    private void OnDisable()
    {
        previousButton.onClick.RemoveListener(ShowPrevious);
        nextButton.onClick.RemoveListener(ShowNext);
        previousAction.performed -= OnPreviousPerformed;
        nextAction.performed -= OnNextPerformed;
        previousAction.Disable();
        nextAction.Disable();

        // A running slide stops with the object; start clean the next time.
        isChanging = false;
    }

    private void OnDestroy()
    {
        previousAction.Dispose();
        nextAction.Dispose();
    }

    private void Start()
    {
        if (catalog.Count == 0)
        {
            return;
        }

        index = catalog.IndexOf(SaveSystem.Data.planeSkinId);
        previewAnimator.runtimeAnimatorController = catalog[index].preview;
    }

    // The arrow buttons play their sound through UiSound; the keyboard and gamepad have no button, so the same sound is played here.
    private void OnPreviousPerformed(InputAction.CallbackContext context) => ChangeWithSound(-1);

    private void OnNextPerformed(InputAction.CallbackContext context) => ChangeWithSound(1);

    private void ChangeWithSound(int direction)
    {
        if (Change(direction) && AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(SoundId.SelectorTap);
        }
    }

    private void ShowPrevious() => Change(-1);

    private void ShowNext() => Change(1);

    /// <summary>Starts the change; returns false if it was ignored.</summary>
    private bool Change(int direction)
    {
        // One change at a time keeps the animation clean; it only lasts about a third of a second.
        if (isChanging || catalog.Count < 2)
        {
            return false;
        }

        StartCoroutine(ChangeRoutine(direction));
        return true;
    }

    private IEnumerator ChangeRoutine(int direction)
    {
        isChanging = true;

        // The old plane leaves towards the side the player pressed: right arrow = it slides to the left, the next one comes from the right.
        yield return Slide(0f, -direction * slideDistance, 1f, 0f, leaveSeconds);

        index = (index + direction + catalog.Count) % catalog.Count;
        previewAnimator.runtimeAnimatorController = catalog[index].preview;
        SaveSystem.Data.planeSkinId = catalog[index].id;
        SaveSystem.Save();

        yield return Slide(direction * slideDistance, 0f, 0f, 1f, enterSeconds);

        isChanging = false;
    }

    private IEnumerator Slide(float fromX, float toX, float fromAlpha, float toAlpha, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
            SetLook(Mathf.Lerp(fromX, toX, t), Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetLook(toX, toAlpha);
    }

    private void SetLook(float x, float alpha)
    {
        Vector2 position = visual.anchoredPosition;
        position.x = x;
        visual.anchoredPosition = position;

        Color color = visualImage.color;
        color.a = alpha;
        visualImage.color = color;
    }
}
