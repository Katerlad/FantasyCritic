using FantasyCritic.Lib.Discord.Interfaces;

namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
public record NewGameNewsRecord(MasterGame MasterGame, LocalDate CurrentDate): IGameNewsRecord;
