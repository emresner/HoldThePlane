using UnityEngine;

/// <summary>
/// The sounds of a run: crash, star pickup, every 100 points, the resume countdown and a new best score.
/// It only listens to events of the other systems and asks the <see cref="AudioManager"/> to play; none of them know about sound.
/// (The UI clicks are separate <see cref="UiSound"/> components.)
/// </summary>
public class GameAudio : MonoBehaviour
{
    [SerializeField] private RunController runController;
    [SerializeField] private PlaneCrash plane;
    [SerializeField] private PlaneCollector collector;
    [SerializeField] private ScoreKeeper scoreKeeper;
    [SerializeField] private ResumeCountdown countdown;

    [Header("Score milestone")]
    [Tooltip("A sound plays every time the score passes a multiple of this.")]
    [SerializeField, Min(1)] private int milestoneEvery = 100;

    [Header("New best")]
    [Tooltip("Seconds after the crash before the new-best sound. Keep it the same as the Game Over panel's Show Delay, so the sound arrives together with the NEW badge.")]
    [SerializeField, Min(0f)] private float newBestDelay = 0.6f;

    [Header("Countdown")]
    [Tooltip("Pitch of the first tick; every following tick is one step higher (3 - 2 - 1 rises).")]
    [SerializeField, Range(0.5f, 2f)] private float firstTickPitch = 0.9f;
    [SerializeField, Range(0f, 0.5f)] private float tickPitchStep = 0.1f;

    private int lastMilestone;
    private GameState previousState;

    private void OnEnable()
    {
        plane.Crashed += HandleCrashed;
        collector.Collected += HandleCollected;
        scoreKeeper.ScoreChanged += HandleScoreChanged;
        countdown.Ticked += HandleTicked;
        runController.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        plane.Crashed -= HandleCrashed;
        collector.Collected -= HandleCollected;
        scoreKeeper.ScoreChanged -= HandleScoreChanged;
        countdown.Ticked -= HandleTicked;
        runController.StateChanged -= HandleStateChanged;
    }

    private void HandleCrashed()
    {
        Play(SoundId.Crash);
    }

    private void HandleCollected(Collectible collectible)
    {
        Play(SoundId.StarPickup);
    }

    private void HandleScoreChanged(int score)
    {
        // Division, not equality: a star can jump the score over the multiple (for example 95 to 105).
        int reached = score / milestoneEvery;
        if (reached > lastMilestone)
        {
            lastMilestone = reached;
            Play(SoundId.ScoreMilestone);
        }
    }

    private void HandleTicked(int stepIndex)
    {
        Play(SoundId.CountdownTick, firstTickPitch + tickPitchStep * stepIndex);
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Playing && previousState == GameState.Resuming)
        {
            Play(SoundId.CountdownGo);
        }
        else if (state == GameState.GameOver && runController.IsNewBest)
        {
            Play(SoundId.NewBest, 1f, newBestDelay);
        }

        previousState = state;
    }

    private static void Play(SoundId id, float pitchMultiplier = 1f, float delay = 0f)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(id, pitchMultiplier, delay);
        }
    }
}
