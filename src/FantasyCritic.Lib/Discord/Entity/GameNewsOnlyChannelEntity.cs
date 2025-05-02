using FantasyCritic.Lib.Discord.Handlers;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Discord.Entity;
public class GameNewsOnlyChannelEntity : IDiscordChannel, IGameNewsReceiver
{
    public ulong GuildID { get; }
    public ulong ChannelID { get; }
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public IRelevantGameNewsHandler? RelevantGameNewsHandler { get => GetRelevantGameNewsHandler(); }
    public GameNewsSettingsRecord? GameNewsSettings { get; }
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; }

    public GameNewsOnlyChannelEntity(GameNewsOnlyChannelRecord record)
    {
        GuildID = record.GuildID;
        ChannelID = record.ChannelID;
        GameNewsSettings = record.GameNewsSettings;
        ActiveLeagueYears = new List<LeagueYear>();
    }

    private IRelevantGameNewsHandler? GetRelevantGameNewsHandler()
    {
        if (GameNewsSettings != null)
        {
            return new RelevantGameNewsOnlyHandler(GameNewsSettings,ChannelKey);
        }

        return null;
    }
}
