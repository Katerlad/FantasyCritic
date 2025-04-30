using FantasyCritic.Lib.Discord.Interfaces;

namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
public record EditedGameNewsRecord(MasterGame MasterGame, bool ReleaseStatusChanged, LocalDate CurrentDate): IGameNewsRecord;
