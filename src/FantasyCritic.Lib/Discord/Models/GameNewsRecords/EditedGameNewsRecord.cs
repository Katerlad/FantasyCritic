using FantasyCritic.Lib.Discord.Interfaces;

namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
internal record EditedGameNewsRecord(MasterGame MasterGame, bool ReleaseStatusChanged, LocalDate CurrentDate): IGameNewsRecord;
