using FantasyCritic.Lib.Discord.Handlers;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Discord.Entity;
public class LeagueChannelEntity : IDiscordChannel, IGameNewsReceiver
{

    //Identifiers
    public ulong GuildID { get; }
    public ulong ChannelID { get; }
    public Guid LeagueID  { get; }
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public LeagueGameNewsSettings LeagueGameNewsSettings { get; }
    public GameNewsSettings GameNewsSettings { get; }
    public LeagueYear CurrentYear { get; }
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; set; } 
    public IRelevantGameNewsHandler RelevantGameNewsHandler { get; }

    public ulong? BidAlertRoleID { get; set; } = null;


    public LeagueChannelEntity(LeagueChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = record.GameNewsSettings;
        CurrentYear = record.CurrentYear;
        ActiveLeagueYears = record.ActiveLeagueYears;
        LeagueGameNewsSettings = record.LeagueGameNewsSettings;
        RelevantGameNewsHandler = new RelevantLeagueGameNewsHandler(this);
    }

    public LeagueChannelEntity(MinimalLeagueChannelRecord record, IReadOnlyList<LeagueYear> activeLeagueYears,LeagueYear currentYear, GameNewsSettings gameNewsOnlySettings, LeagueGameNewsSettings leagueGameNewsSettings)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        CurrentYear = currentYear;
        ActiveLeagueYears = activeLeagueYears;
        RelevantGameNewsHandler = new RelevantLeagueGameNewsHandler(this);
        GameNewsSettings = gameNewsOnlySettings;
        LeagueGameNewsSettings = leagueGameNewsSettings;
    }

    public LeagueChannelRecord ToDomain(LeagueYear leagueYear)
    {
        return new LeagueChannelRecord(GuildID, ChannelID, LeagueID, CurrentYear, ActiveLeagueYears, LeagueGameNewsSettings, GameNewsSettings, BidAlertRoleID);
    }

    public MinimalLeagueChannelRecord ToMinimalDomain()
    {
        return new MinimalLeagueChannelRecord(GuildID, ChannelID, LeagueID, BidAlertRoleID);
    }
}
