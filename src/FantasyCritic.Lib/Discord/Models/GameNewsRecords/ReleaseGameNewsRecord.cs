using FantasyCritic.Lib.Discord.Interfaces;

namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
internal record ReleaseGameNewsRecord(MasterGame MasterGame, LocalDate CurrentDate) : IGameNewsRecord;
