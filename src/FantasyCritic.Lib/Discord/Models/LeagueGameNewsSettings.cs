using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.Lib.Discord.Models;
public class LeagueGameNewsSettings
{
    public bool ShowEligibleGameNewsOnly { get; } = false;

    public bool ShowCurrentYearGameNewsOnly { get; } = false;

    public NotableMissSetting NotableMissSetting { get; } = NotableMissSetting.ScoreUpdates;

    public LeagueGameNewsSettings()
    {
    }
    public LeagueGameNewsSettings(bool showEligibleGameNewsOnly, bool showCurrentYearGameNewsOnly, NotableMissSetting notableMissSetting)
    {
        ShowEligibleGameNewsOnly = showEligibleGameNewsOnly;
        ShowCurrentYearGameNewsOnly = showCurrentYearGameNewsOnly;
        NotableMissSetting = notableMissSetting;
    }
}
