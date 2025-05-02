
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.MySQL.Entities;
internal class LeagueGameNewsSettingsEntity
{
    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }
    public Guid LeagueID { get; set; }
    public bool ShowPickedGameNews { get; set; }
    public bool ShowEligibleGameNews { get; set; } 
    public bool ShowCurrentYearGameNewsOnly { get; set; } 
    public string NotableMissSetting { get; set; }

    public LeagueGameNewsSettingsEntity(ulong guildID, ulong channelID,Guid leagueID, bool showPickedGameNews, bool showEligibleGameNews, bool showCurrentYearGameNewsOnly, NotableMissSetting notableMissesSetting)
    {
        GuildID = guildID;
        ChannelID = channelID;
        LeagueID = leagueID;
        ShowPickedGameNews = showPickedGameNews;
        ShowEligibleGameNews = showEligibleGameNews;
        ShowCurrentYearGameNewsOnly = showCurrentYearGameNewsOnly;
        NotableMissSetting = notableMissesSetting.ToString();
    }

    public LeagueGameNewsSettingsEntity(ulong guildID, ulong channelID, Guid leagueID, bool showPickedGameNews, bool showEligibleGameNews, bool showCurrentYearGameNewsOnly, string notableMissSetting)
    {
        GuildID = guildID;
        ChannelID = channelID;
        LeagueID = leagueID;
        ShowPickedGameNews = showPickedGameNews;
        ShowEligibleGameNews = showEligibleGameNews;
        ShowCurrentYearGameNewsOnly = showCurrentYearGameNewsOnly;
        NotableMissSetting = notableMissSetting;
    }

    public LeagueGameNewsSettingsRecord ToRecord()
    {
        return new LeagueGameNewsSettingsRecord(ShowPickedGameNews, ShowEligibleGameNews, ShowCurrentYearGameNewsOnly, FantasyCritic.Lib.Discord.Enums.NotableMissSetting.FromValue(NotableMissSetting));
    }
}
