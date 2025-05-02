using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.Lib.Discord.Models;
public record LeagueGameNewsSettingsRecord
{
    public bool ShowPickedGameNews { get; } = true;
    public bool ShowEligibleGameNews { get; } = true;

    public bool ShowCurrentYearGameNewsOnly { get; } = false;

    public NotableMissSetting NotableMissSetting { get; } = NotableMissSetting.ScoreUpdates;

    public LeagueGameNewsSettingsRecord()
    {
    }
    public LeagueGameNewsSettingsRecord(bool showPickedGameNews,bool showEligibleGameNews, bool showCurrentYearGameNewsOnly, NotableMissSetting notableMissSetting)
    {
        ShowPickedGameNews = showPickedGameNews;
        ShowEligibleGameNews = showEligibleGameNews;
        ShowCurrentYearGameNewsOnly = showCurrentYearGameNewsOnly;
        NotableMissSetting = notableMissSetting;
    }
}
