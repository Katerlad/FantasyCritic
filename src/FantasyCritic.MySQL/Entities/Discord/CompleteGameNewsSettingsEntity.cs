using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.MySQL.Entities.Discord;
internal class CompleteGameNewsSettingsEntity
{
    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }
    public bool EnableGameNews { get; set; } = true;
    public bool? ShowPickedGameNews { get; set; } = null;
    public bool? ShowEligibleGameNews { get; set; } = null;
    public NotableMissSetting? NotableMissSetting { get; set; } = null;
    public bool ShowMightReleaseInYearNews { get; set; } = true;
    public bool ShowWillReleaseInYearNews { get; set; } = true;
    public bool ShowScoreGameNews { get; set; } = true;
    public bool ShowReleasedGameNews { get; set; } = true;
    public bool ShowNewGameNews { get; set; } = true;
    public bool ShowEditedGameNews { get; set; } = true;

    public CompleteGameNewsSettings ToDomain(List<MasterGameTag> skippedTags)
    {
        return new CompleteGameNewsSettings()
        {
            EnableGameNews = EnableGameNews,
            ShowPickedGameNews = ShowPickedGameNews,
            ShowEligibleGameNews = ShowEligibleGameNews,
            NotableMissSetting = NotableMissSetting,
            ShowMightReleaseInYearNews = ShowMightReleaseInYearNews,
            ShowWillReleaseInYearNews = ShowWillReleaseInYearNews,
            ShowScoreGameNews = ShowScoreGameNews,
            ShowReleasedGameNews = ShowReleasedGameNews,
            ShowNewGameNews = ShowNewGameNews,
            ShowEditedGameNews = ShowEditedGameNews,
            SkippedTags = skippedTags,
        };
    }
}
