using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the flow of one run in the Game scene: GetReady → Playing → GameOver (with Paused / Resuming in between while playing), and what each stage means for the world and the plane.
/// Screens never talk to each other; they listen to <see cref="StateChanged"/> and call <see cref="Replay"/> / <see cref="ReturnToMenu"/>.
/// Replaying reloads the scene (the simplest way to reset everything at once); the menu is a scene of its own.
/// </summary>
public class RunController : MonoBehaviour
{
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private PlaneCrash plane;
    [SerializeField] private ScoreKeeper scoreKeeper;
    [Tooltip("Name of the menu scene (it must be in the Build Settings).")]
    [SerializeField] private string menuSceneName = "Menu";

    /// <summary>Raised after every state change, with the new state.</summary>
    public event Action<GameState> StateChanged;

    // Greyed out in the Inspector: watch them while playing.
    [field: SerializeField, DebugReadOnly] public GameState State { get; private set; }

    /// <summary>Score of the run that just ended (valid in GameOver).</summary>
    [field: SerializeField, DebugReadOnly] public int FinalScore { get; private set; }

    /// <summary>Best score on record, including this run if it was a record (valid in GameOver).</summary>
    [field: SerializeField, DebugReadOnly] public int BestScore { get; private set; }

    [field: SerializeField, DebugReadOnly] public bool IsNewBest { get; private set; }

    private IFlightInput flightInput;
    private PlaneMovement planeMovement;
    private int stateEnterFrame;

    private void Awake()
    {
        flightInput = plane.GetComponent<IFlightInput>();
        planeMovement = plane.GetComponent<PlaneMovement>();
        BestScore = BestScoreStore.Load();
        FinalScore = 0;
        IsNewBest = false;
    }

    private void OnEnable()
    {
        plane.Crashed += HandleCrashed;
    }

    private void OnDisable()
    {
        plane.Crashed -= HandleCrashed;
    }

    private void OnDestroy()
    {
        // Whatever way this scene ends, never leave the game frozen for the next one.
        Time.timeScale = 1f;
    }

    private void Start()
    {
        // Start, not Awake: the screens subscribe in their own OnEnable first and must hear the first state.
        SetState(GameState.GetReady);
    }

    private void Update()
    {
        // Ignore a press that arrives in the same frame the state changed.
        if (Time.frameCount == stateEnterFrame)
        {
            return;
        }

        if (State == GameState.GetReady && flightInput.PressedThisFrame)
        {
            SetState(GameState.Playing);
        }
    }

    /// <summary>Freezes the run and opens the pause menu. Only works while playing.</summary>
    public void Pause()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        // timeScale 0 stops physics, the scrolling world, spawning and animations in one go.
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    /// <summary>Starts the 3-2-1 countdown. The world stays frozen until <see cref="FinishResume"/>.</summary>
    public void Resume()
    {
        if (State == GameState.Paused)
        {
            SetState(GameState.Resuming);
        }
    }

    /// <summary>Called by the countdown when it reaches zero: the world moves again and the plane obeys the input.</summary>
    public void FinishResume()
    {
        if (State != GameState.Resuming)
        {
            return;
        }

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    /// <summary>Pause while playing, resume while paused (used by the keyboard shortcut).</summary>
    public void TogglePause()
    {
        if (State == GameState.Playing)
        {
            Pause();
        }
        else if (State == GameState.Paused)
        {
            Resume();
        }
    }

    /// <summary>Reloads the Game scene and goes straight to GetReady.</summary>
    public void Replay()
    {
        Time.timeScale = 1f;
        GameSession.StartReplay();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Leaves the Game scene for the menu scene.</summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

#if UNITY_EDITOR
    // Right-click the component header (or the ⋮ menu) in the Inspector to run it. Editor only: it is not part of builds.
    [ContextMenu("Reset Best Score")]
    private void ResetBestScore()
    {
        BestScoreStore.Clear();
        BestScore = 0;
        IsNewBest = false;
        Debug.Log("Best score cleared.", this);
    }
#endif

    private void HandleCrashed()
    {
        if (State == GameState.Playing)
        {
            SetState(GameState.GameOver);
        }
    }

    private void SetState(GameState newState)
    {
        State = newState;
        stateEnterFrame = Time.frameCount;

        // The plane only obeys the input while playing; with gravity off it simply hangs still otherwise.
        planeMovement.enabled = newState == GameState.Playing;

        switch (newState)
        {
            case GameState.Playing:
                worldScroller.Begin();
                break;
            case GameState.GameOver:
                worldScroller.Stop();
                FinalScore = scoreKeeper.Score;
                IsNewBest = BestScoreStore.TrySetBest(FinalScore);
                BestScore = BestScoreStore.Load();
                break;
        }

        StateChanged?.Invoke(newState);
    }
}
