using UnityEngine;

/// <summary>
/// Small notes that must survive a scene change (a scene reload wipes every normal object).
/// For now: did the player arrive from the menu, or by pressing Replay? The Get Ready screen explains the controls only the first time.
/// </summary>
public static class GameSession
{
    /// <summary>True when the current Game scene was started with Replay, false when it came from the menu (or was started directly in the editor).</summary>
    public static bool IsReplay { get; private set; }

    public static void StartFromMenu()
    {
        IsReplay = false;
    }

    public static void StartReplay()
    {
        IsReplay = true;
    }

    // Static values survive Play Mode runs when "Reload Domain" is switched off in the editor, so start every run clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLaunch()
    {
        IsReplay = false;
    }
}
