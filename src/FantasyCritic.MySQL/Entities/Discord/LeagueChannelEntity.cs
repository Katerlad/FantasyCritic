using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.MySQL.Entities.Discord;
internal class LeagueChannelEntity
{
    public LeagueChannelEntity()
    {

    }

    public LeagueChannelEntity(ulong guildID, ulong channelID, Guid leagueID, bool sendLeagueMasterGameUpdates, NotableMissesSetting notableMissesSetting, ulong? bidAlertRoleID)
    {
        GuildID = guildID;
        ChannelID = channelID;
        LeagueID = leagueID;
        SendLeagueMasterGameUpdates = sendLeagueMasterGameUpdates;
        NotableMissesSetting = notableMissesSetting;
        BidAlertRoleID = bidAlertRoleID;
    }

    public ulong ChannelID { get; set; }
    public Guid LeagueID { get; set; }
    public ulong GuildID { get; set; }
    public bool SendLeagueMasterGameUpdates { get; set; }
    public NotableMissesSetting NotableMissesSetting { get; set; }
    public ulong? BidAlertRoleID { get; set; }

    public LeagueChannel ToDomain(LeagueYear leagueYear)
    {
        return new LeagueChannel(leagueYear, GuildID, ChannelID, SendLeagueMasterGameUpdates, NotableMissesSetting, BidAlertRoleID);
    }

    public MinimalLeagueChannel ToMinimalDomain()
    {
        return new MinimalLeagueChannel(LeagueID, GuildID, ChannelID, SendLeagueMasterGameUpdates, NotableMissesSetting, BidAlertRoleID);
    }
}
