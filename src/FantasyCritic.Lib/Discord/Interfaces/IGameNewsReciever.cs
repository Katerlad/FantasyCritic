namespace FantasyCritic.Lib.Discord.Interfaces;
internal interface IGameNewsReciever
{
    //Identifiers
    ulong GuildID { get; }
    ulong ChannelID { get; }

    IReadOnlyList<LeagueYear> ActiveLeagueYears { get; }
    GameNewsSettings GameNewsSettings { get; }
    IRelevantGameNewsHandler RelevantGameNewsHandler { get; }
}
