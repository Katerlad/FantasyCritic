
using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.MySQL.Entities;
internal class LeagueGameNewsSettingEntity
{
    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }
    public Guid LeagueID { get; set; } 
    public bool SendEligibleGameNewsOnly { get; set; } 
    public bool SendCurrentYearGameNewsOnly { get; set; } 
    public string NotableMissSetting { get; set; }

    public LeagueGameNewsSettingEntity(ulong guildID, ulong channelID,Guid leagueID, bool sendEligibleGameNewsOnly, bool sendCurrentYearGameNewsOnly, NotableMissSetting notableMissesSetting)
    {
        GuildID = guildID;
        ChannelID = channelID;
        LeagueID = leagueID;
        SendEligibleGameNewsOnly = sendEligibleGameNewsOnly;
        SendCurrentYearGameNewsOnly = sendCurrentYearGameNewsOnly;
        NotableMissSetting = notableMissesSetting.ToString();
    }
}
