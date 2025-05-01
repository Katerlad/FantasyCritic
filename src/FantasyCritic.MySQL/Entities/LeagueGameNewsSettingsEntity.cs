
using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.MySQL.Entities;
internal class LeagueGameNewsSettingsEntity
{
    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }
    public Guid LeagueID { get; set; } 
    public bool ShowEligibleGameNewsOnly { get; set; } 
    public bool ShowCurrentYearGameNewsOnly { get; set; } 
    public string NotableMissSetting { get; set; }

    public LeagueGameNewsSettingsEntity(ulong guildID, ulong channelID,Guid leagueID, bool showEligibleGameNewsOnly, bool showCurrentYearGameNewsOnly, NotableMissSetting notableMissesSetting)
    {
        GuildID = guildID;
        ChannelID = channelID;
        LeagueID = leagueID;
        ShowEligibleGameNewsOnly = showEligibleGameNewsOnly;
        ShowCurrentYearGameNewsOnly = showCurrentYearGameNewsOnly;
        NotableMissSetting = notableMissesSetting.ToString();
    }

    public LeagueGameNewsSettingsEntity(ulong guildID, ulong channelID, Guid leagueID, bool showEligibleGameNewsOnly, bool showCurrentYearGameNewsOnly, string notableMissSetting)
    {
        GuildID = guildID;
        ChannelID = channelID;
        LeagueID = leagueID;
        ShowEligibleGameNewsOnly = showEligibleGameNewsOnly;
        ShowCurrentYearGameNewsOnly = showCurrentYearGameNewsOnly;
        NotableMissSetting = notableMissSetting;
    }
}
