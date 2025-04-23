

namespace FantasyCritic.Lib.Discord.Interfaces;
internal interface IGameNewsRecord
{
    MasterGame MasterGame { get; }
    LocalDate CurrentDate { get; }
}
