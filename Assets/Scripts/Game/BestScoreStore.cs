/// <summary>
/// The best-score rules (only a higher score replaces the stored one). Where and how it is stored is <see cref="SaveSystem"/>'s job.
/// </summary>
public static class BestScoreStore
{
    public static int Load()
    {
        return SaveSystem.Data.bestScore;
    }

    /// <summary>Forgets the stored best score. Other saved data stays.</summary>
    public static void Clear()
    {
        SaveSystem.Data.bestScore = 0;
        SaveSystem.Save();
    }

    /// <summary>Saves <paramref name="score"/> if it beats the stored best. Returns true when it did.</summary>
    public static bool TrySetBest(int score)
    {
        if (score <= Load())
        {
            return false;
        }

        SaveSystem.Data.bestScore = score;
        SaveSystem.Save();
        return true;
    }
}
