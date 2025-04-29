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
    public GameNewsSettings GameNewsSettings { get; }
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; set; } 
    public IRelevantGameNewsHandler RelevantGameNewsHandler { get; } 
    

    public LeagueChannelEntity(LeagueChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = record.gameNewsSettings;
        ActiveLeagueYears = record.ActiveLeagueYears;
        RelevantGameNewsHandler = new RelevantLeagueGameNewsHandler(this);
    }

    public LeagueChannelEntity(MinimalLeagueChannelRecord record, IReadOnlyList<LeagueYear> activeLeagueYears)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = new GameNewsSettings();
        ActiveLeagueYears = activeLeagueYears;
        RelevantGameNewsHandler = new RelevantLeagueGameNewsHandler(this);
    }
}
