namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
internal record ReleaseGameNewsRecord(MasterGame masterGame, IReadOnlyList<LeagueYear>? activeLeagueYears);
