using FantasyCritic.Lib.Discord.Handlers;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Discord.Entity;
public class GameNewsOnlyChannelEntity : IDiscordChannel, IGameNewsReceiver
{
    public ulong GuildID { get; }
    public ulong ChannelID { get; }
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public IRelevantGameNewsHandler RelevantGameNewsHandler { get; }
    public GameNewsSettings GameNewsSettings { get; }
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; }

    public GameNewsOnlyChannelEntity(GameNewsOnlyChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = record.GameNewsSettings;
        RelevantGameNewsHandler = new RelevantGameNewsOnlyHandler(record.GameNewsSettings,ChannelKey);
        ActiveLeagueYears = new List<LeagueYear>();
    }
}
