using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.Lib.Discord.Models;
public record LeagueGameNewsSettingsRecord
{
    public bool ShowPickedGameNews { get; } = true;
    public bool ShowEligibleGameNews { get; } = true;

    public NotableMissSetting NotableMissSetting { get; } = NotableMissSetting.ScoreUpdates;

    public LeagueGameNewsSettingsRecord()
    {
    }

    public LeagueGameNewsSettingsRecord(bool showPickedGameNews,bool showEligibleGameNews, NotableMissSetting notableMissSetting)
    {
        ShowPickedGameNews = showPickedGameNews;
        ShowEligibleGameNews = showEligibleGameNews;
        NotableMissSetting = notableMissSetting;
    }
}
