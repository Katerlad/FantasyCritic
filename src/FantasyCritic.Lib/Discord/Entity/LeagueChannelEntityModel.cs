using FantasyCritic.Lib.Discord.Handlers;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Discord.Entity;
public class LeagueChannelEntityModel : IDiscordChannel, IGameNewsReceiver
{

    //Identifiers
    public ulong GuildID { get; }
    public ulong ChannelID { get; }
    public Guid LeagueID  { get; }
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public LeagueGameNewsSettingsRecord? LeagueGameNewsSettings { get; }
    public GameNewsSettingsRecord? GameNewsSettings { get; }
    public LeagueYear CurrentYear { get; }
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; set; } 
    public IRelevantGameNewsHandler? RelevantGameNewsHandler { get => GetRelevantGameNewsHandler(); }

    public ulong? BidAlertRoleID { get; set; } = null;


    public LeagueChannelEntityModel(LeagueChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        LeagueID = record.LeagueID;
        CurrentYear = record.CurrentYear;
        ActiveLeagueYears = record.ActiveLeagueYears;
        LeagueGameNewsSettings = record.LeagueGameNewsSettings;
    }

    public LeagueChannelEntityModel(MinimalLeagueChannelRecord record, IReadOnlyList<LeagueYear> activeLeagueYears,LeagueYear currentYear, LeagueGameNewsSettingsRecord? leagueGameNewsSettings)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        LeagueID = record.LeagueID;
        CurrentYear = currentYear;
        ActiveLeagueYears = activeLeagueYears;
        LeagueGameNewsSettings = leagueGameNewsSettings;
    }

    public LeagueChannelRecord ToDomain(LeagueYear leagueYear)
    {
        return new LeagueChannelRecord(GuildID, ChannelID, LeagueID, CurrentYear, ActiveLeagueYears, LeagueGameNewsSettings, BidAlertRoleID);
    }

    public MinimalLeagueChannelRecord ToMinimalDomain()
    {
        return new MinimalLeagueChannelRecord(GuildID, ChannelID, LeagueID, BidAlertRoleID);
    }

    private IRelevantGameNewsHandler? GetRelevantGameNewsHandler()
    {
        if (LeagueGameNewsSettings != null && GameNewsSettings != null)
        {
            return new RelevantLeagueGameNewsHandler(this);
        }
        else if (GameNewsSettings != null)
        {
            return new RelevantGameNewsOnlyHandler(GameNewsSettings, ChannelKey);
        }

        return null;
    }

}
