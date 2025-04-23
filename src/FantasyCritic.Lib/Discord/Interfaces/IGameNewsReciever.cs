
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Discord.Interfaces;
internal interface IGameNewsReciever
{
    AdvancedGameNewsSettings GameNewsSettings { get; }
    IRelevantGameNewsHandler RelevantGameNewsHandler { get; }
}
