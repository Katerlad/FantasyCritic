using FantasyCritic.Lib.Discord.Interfaces;

namespace FantasyCritic.Lib.Discord.Models.GameNewsRecords;
internal record ScoreGameNewsRecord(MasterGame MasterGame,decimal? NewScore, decimal? OldScore, LocalDate CurrentDate) : IGameNewsRecord;
