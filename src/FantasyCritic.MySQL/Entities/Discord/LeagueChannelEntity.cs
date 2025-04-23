using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.MySQL.Entities.Discord;
internal class LeagueChannelEntity
{

    public LeagueChannelEntity(LeagueChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        LeagueID = record.LeagueID;
        SendLeagueMasterGameUpdates = record.SendLeagueMasterGameUpdates;
        NotableMissesSetting = record.NotableMissesSetting;
        BidAlertRoleID = record.BidAlertRoleID;
        SendEligibleSlotGameNewsOnly = record.SendEligibleSlotGameNewsOnly;
        ActiveLeagueYears = record.ActiveLeagueYears;
        CurrentLeagueYear = record.CurrentLeagueYear;
    }

    public ulong ChannelID { get; set; }
    public Guid LeagueID { get; set; }
    public ulong GuildID { get; set; }
    public bool SendLeagueMasterGameUpdates { get; set; }
    public bool SendEligibleSlotGameNewsOnly { get; set; }
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; set; } = new List<LeagueYear>();
    public LeagueYear CurrentLeagueYear { get; set; }
    public NotableMissSetting NotableMissesSetting { get; set; } = NotableMissSetting.ScoreUpdates;
    public GameNewsSettings GameNewsSettings { get; set; } = new GameNewsSettings();
    public ulong? BidAlertRoleID { get; set; }

    public LeagueChannelRecord ToDomain(LeagueYear leagueYear)
    {
        return new LeagueChannelRecord(ActiveLeagueYears, CurrentLeagueYear, GuildID, ChannelID, LeagueID, SendLeagueMasterGameUpdates, SendEligibleSlotGameNewsOnly, NotableMissesSetting, GameNewsSettings, BidAlertRoleID);
    }

    public MinimalLeagueChannelRecord ToMinimalDomain()
    {
        return new MinimalLeagueChannelRecord(LeagueID, GuildID, ChannelID, SendLeagueMasterGameUpdates, NotableMissesSetting, BidAlertRoleID);
    }
}
