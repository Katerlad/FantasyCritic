namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
internal record EditedGameNewsRecord(MasterGame masterGame, bool releaseStatusChanged, LocalDate currentDate);
