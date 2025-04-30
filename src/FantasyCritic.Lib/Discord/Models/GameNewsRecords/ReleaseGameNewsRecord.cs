using FantasyCritic.Lib.Discord.Interfaces;

namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
public record ReleaseGameNewsRecord(MasterGame MasterGame, LocalDate CurrentDate) : IGameNewsRecord;
