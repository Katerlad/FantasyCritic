namespace FantasyCritic.Lib.Discord.Interfaces;
public interface IGameNewsReceiver
{
    //Identifiers
    ulong GuildID { get; }
    ulong ChannelID { get; }

    IReadOnlyList<LeagueYear> ActiveLeagueYears { get; }
    GameNewsSettings GameNewsSettings { get; }
    IRelevantGameNewsHandler RelevantGameNewsHandler { get; }
}
