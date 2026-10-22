namespace Humanesque.Models;

public class SessionStats
{
    public int TotalCharacters { get; set; }
    public int CharactersTyped { get; set; }
    public int TyposIntroduced { get; set; }
    public int BackspacesDispatched { get; set; }
    public double CurrentWpm { get; set; }
    public double AverageWpm { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }

    public double ProgressPercentage => TotalCharacters > 0
        ? Math.Clamp((double)CharactersTyped / TotalCharacters * 100.0, 0.0, 100.0)
        : 0.0;
}
