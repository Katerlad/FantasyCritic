using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.MySQL.Entities.Discord;
internal class LeagueChannelEntity
{
    public LeagueChannelEntity()
    {

    }

    public LeagueChannelEntity(ulong guildID, ulong channelID, Guid leagueID, bool showPickedGameNews, bool showEligibleGameNews, string notableMissSetting, ulong? bidAlertRoleID)
    {
        GuildID = guildID;
        ChannelID = channelID;
        LeagueID = leagueID;
        ShowPickedGameNews = showPickedGameNews;
        ShowEligibleGameNews = showEligibleGameNews;
        NotableMissSetting = notableMissSetting;
        BidAlertRoleID = bidAlertRoleID;
    }

    public ulong ChannelID { get; set; }
    public Guid LeagueID { get; set; }
    public ulong GuildID { get; set; }
    public bool ShowPickedGameNews { get; set; }
    public bool ShowEligibleGameNews { get; set; }
    public string NotableMissSetting { get; set; } = null!;
    public ulong? BidAlertRoleID { get; set; }

    public LeagueChannelRecord ToDomain(LeagueYear leagueYear, IReadOnlyList<LeagueYear> activeLeagueYears)
    {
        var leagueGameNewsSettings = new LeagueGameNewsSettingsRecord(ShowPickedGameNews, ShowEligibleGameNews, Lib.Discord.Enums.NotableMissSetting.FromValue(NotableMissSetting));
        return new LeagueChannelRecord(GuildID, ChannelID, LeagueID, leagueYear, activeLeagueYears, leagueGameNewsSettings, BidAlertRoleID);
    }

    public MinimalLeagueChannelRecord ToMinimalDomain()
    {
        return new MinimalLeagueChannelRecord(GuildID, ChannelID, LeagueID, BidAlertRoleID);
    }
}
