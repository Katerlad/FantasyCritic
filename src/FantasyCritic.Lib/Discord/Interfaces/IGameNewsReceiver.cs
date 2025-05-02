namespace FantasyCritic.Lib.Discord.Interfaces;
public interface IGameNewsReceiver
{
    //Identifiers
    ulong GuildID { get; }
    ulong ChannelID { get; }

    IReadOnlyList<LeagueYear> ActiveLeagueYears { get; }
    GameNewsSettingsRecord? GameNewsSettings { get; }
    IRelevantGameNewsHandler? RelevantGameNewsHandler { get; }
}
