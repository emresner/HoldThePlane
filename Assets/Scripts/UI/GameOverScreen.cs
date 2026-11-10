using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Game Over panel: waits a moment after the crash, then fades in with the medal, the best score and the Replay / Menu buttons.
/// The score itself is a <see cref="ScoreDisplay"/> inside the panel that counts up when the panel is switched on.
/// It only listens to <see cref="RunController.StateChanged"/> and calls back <c>Replay</c> / <c>ReturnToMenu</c>.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private RunController runController;
    [Tooltip("Everything that is shown (inactive until the panel appears).")]
    [SerializeField] private GameObject content;
    [SerializeField] private CanvasGroup contentGroup;

    [Header("Result")]
    [SerializeField] private Image medalImage;
    [SerializeField] private MedalTable medals;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private GameObject newBestBadge;

    [Header("Buttons")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;

    [Header("Timing")]
    [Tooltip("Seconds between the crash and the panel, so the crash itself can be seen.")]
    [SerializeField, Min(0f)] private float showDelay = 0.6f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.25f;

    private void Awake()
    {
        content.SetActive(false);
    }

    private void OnEnable()
    {
        runController.StateChanged += HandleStateChanged;
        replayButton.onClick.AddListener(runController.Replay);
        menuButton.onClick.AddListener(runController.ReturnToMenu);
    }

    private void OnDisable()
    {
        runController.StateChanged -= HandleStateChanged;
        replayButton.onClick.RemoveListener(runController.Replay);
        menuButton.onClick.RemoveListener(runController.ReturnToMenu);
    }

    private void HandleStateChanged(GameState state)
    {
        StopAllCoroutines();
        if (state == GameState.GameOver)
        {
            StartCoroutine(ShowAfterDelay());
        }
        else
        {
            content.SetActive(false);
        }
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);

        // Fill everything in first: switching the content on starts the score count-up.
        medalImage.sprite = medals.SpriteFor(runController.FinalScore);
        bestScoreText.text = runController.BestScore.ToString();
        newBestBadge.SetActive(runController.IsNewBest);
        contentGroup.alpha = 0f;
        content.SetActive(true);

        for (float time = 0f; time < fadeSeconds; time += Time.deltaTime)
        {
            contentGroup.alpha = time / fadeSeconds;
            yield return null;
        }

        contentGroup.alpha = 1f;
    }
}
