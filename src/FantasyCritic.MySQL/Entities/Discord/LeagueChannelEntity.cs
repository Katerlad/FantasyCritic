

namespace FantasyCritic.MySQL.Entities.Discord;
internal class LeagueChannelEntity
{
    public LeagueChannelEntity(MinimalLeagueChannelRecord record, List<LeagueYear> activeLeagueYears, LeagueYear currentYear)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        LeagueID = record.LeagueID;
        ActiveLeagueYears = activeLeagueYears;
        CurrentLeagueYear = currentYear;
    }
    public LeagueChannelEntity(LeagueChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        LeagueID = record.LeagueID;
        BidAlertRoleID = record.BidAlertRoleID;
        ActiveLeagueYears = record.ActiveLeagueYears;
        CurrentLeagueYear = record.CurrentLeagueYear;
    }

    public ulong ChannelID { get; set; } 
    public Guid LeagueID { get; set; } 
    public ulong GuildID { get; set; }
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; set; } = new List<LeagueYear>();
    public LeagueYear CurrentLeagueYear { get; set; }
    public GameNewsSettings GameNewsSettings { get; set; } = new GameNewsSettings();
    public ulong? BidAlertRoleID { get; set; }

    public LeagueChannelRecord ToDomain(LeagueYear leagueYear)
    {
        return new LeagueChannelRecord(ActiveLeagueYears, CurrentLeagueYear, GuildID, ChannelID, LeagueID, GameNewsSettings, BidAlertRoleID);
    }

    public MinimalLeagueChannelRecord ToMinimalDomain()
    {
        return new MinimalLeagueChannelRecord(GuildID, ChannelID, LeagueID, BidAlertRoleID);
    }
}
