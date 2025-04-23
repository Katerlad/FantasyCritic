using FantasyCritic.Lib.Discord.Handlers;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Discord.Entity;
internal class GameNewsOnlyChannelEntity : IDiscordChannel, IGameNewsReciever
{
    public ulong GuildID { get; }
    public ulong ChannelID { get; }
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public IRelevantGameNewsHandler RelevantGameNewsHandler { get; }
    public AdvancedGameNewsSettings GameNewsSettings { get; }

    public GameNewsOnlyChannelEntity(GameNewsOnlyChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = record.AdvancedGameNewsSettings;
        RelevantGameNewsHandler = new RelevantGameNewsOnlyHandler(record.AdvancedGameNewsSettings);
    }
}
