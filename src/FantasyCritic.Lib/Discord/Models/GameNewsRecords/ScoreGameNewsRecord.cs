namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
internal record ScoreGameNewsRecord(MasterGame masterGame,decimal? criticScore, decimal? oldScore, LocalDate currentDate);
