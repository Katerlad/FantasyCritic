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
    public NotableMissesSetting NotableMissSetting { get; set; }
    public AdvancedGameNewsSettings GameNewsSettings { get; }
    public IRelevantGameNewsHandler RelevantGameNewsHandler { get; } 
    

    public LeagueChannelEntity(LeagueChannelRecord record)
    {
        GameNewsSettings = record.gameNewsSettings;
        RelevantGameNewsHandler = new RelevantLeagueGameNewsHandler(record.gameNewsSettings);
    }
}
