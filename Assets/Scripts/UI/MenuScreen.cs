using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The main menu's behaviour (it lives in its own scene): the Play button loads the Game scene and the best score is shown.
/// </summary>
public class MenuScreen : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [Tooltip("Shown only when there is a best score to show.")]
    [SerializeField] private GameObject bestScoreRow;
    [SerializeField] private TMP_Text bestScoreText;
    [Tooltip("Name of the game scene (it must be in the Build Settings).")]
    [SerializeField] private string gameSceneName = "Game";

    private void OnEnable()
    {
        playButton.onClick.AddListener(Play);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(Play);
    }

    private void Start()
    {
        int best = BestScoreStore.Load();
        bestScoreRow.SetActive(best > 0);
        bestScoreText.text = "BEST SCORE " + best;

        // Select Play so Enter / gamepad confirm can press it without a mouse.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }
    }

    private void Play()
    {
        GameSession.StartFromMenu();
        SceneManager.LoadScene(gameSceneName);
    }
}
