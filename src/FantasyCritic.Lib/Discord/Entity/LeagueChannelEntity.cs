using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Handlers;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Discord.Entity;
internal class LeagueChannelEntity : IDiscordChannel, IGameNewsReciever
{

    //Identifiers
    public ulong GuildID { get; }
    public ulong ChannelID { get; }
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);

    //GameNews
    public bool LeagueGameNewsEnabled { get; set; }

    public bool SendEligibleSlotGameNewsOnly { get; set; }
    public NotableMissSetting NotableMissSetting { get; set; }
    public GameNewsSettings GameNewsSettings { get; }

    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; set; } 
    public IRelevantGameNewsHandler RelevantGameNewsHandler { get; } 
    

    public LeagueChannelEntity(LeagueChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = record.gameNewsSettings;
        LeagueGameNewsEnabled = record.SendLeagueMasterGameUpdates;
        NotableMissSetting = record.NotableMissesSetting;
        SendEligibleSlotGameNewsOnly = record.SendEligibleSlotGameNewsOnly;
        ActiveLeagueYears = record.ActiveLeagueYears;
        RelevantGameNewsHandler = new RelevantLeagueGameNewsHandler(this);
    }

    public LeagueChannelEntity(MinimalLeagueChannelRecord record, IReadOnlyList<LeagueYear> activeLeagueYears)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = new GameNewsSettings();
        LeagueGameNewsEnabled = record.SendLeagueMasterGameUpdates;
        NotableMissSetting = record.NotableMissesSetting;
        ActiveLeagueYears = activeLeagueYears;
        RelevantGameNewsHandler = new RelevantLeagueGameNewsHandler(this);
    }
}
