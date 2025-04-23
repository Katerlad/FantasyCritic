using FantasyCritic.Lib.Discord.Models.GameNewsRecords;

namespace FantasyCritic.Lib.Discord.Interfaces;
internal interface IRelevantGameNewsHandler
{
    public bool IsNewGameNewsRelevant(NewGameNewsRecord newsRecord);
    public bool IsEditedGameNewsRelevant(EditedGameNewsRecord newsRecord);
    public bool IsReleasedGameNewsRelevant(ReleaseGameNewsRecord newsRecord);
    public bool IsScoreGameNewsRelevant(ScoreGameNewsRecord newsRecord);
}
