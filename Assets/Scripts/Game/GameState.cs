/// <summary>The stages one play session moves through. <see cref="RunController"/> owns the current one.</summary>
/// <remarks>New states are appended at the end: the screens store these as numbers in scenes and prefabs, so reordering would silently change what they listen to.</remarks>
public enum GameState
{
    /// <summary>World frozen, waiting for the first press so the player starts when ready.</summary>
    GetReady,

    /// <summary>The run is on: world scrolls, the plane obeys the input.</summary>
    Playing,

    /// <summary>The plane crashed: world frozen, result is shown.</summary>
    GameOver,

    /// <summary>The player stopped the run: everything is frozen and the pause menu is shown.</summary>
    Paused,

    /// <summary>Back from the pause menu: still frozen while the 3-2-1 counts down, then the run goes on.</summary>
    Resuming
}
