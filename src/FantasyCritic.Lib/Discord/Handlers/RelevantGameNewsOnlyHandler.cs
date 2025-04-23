using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Discord.Models.GameNewsRecords;


namespace FantasyCritic.Lib.Discord.Handlers;
internal class RelevantGameNewsOnlyHandler : IRelevantGameNewsHandler
{
    private AdvancedGameNewsSettings _gameNewsSettings;
    public RelevantGameNewsOnlyHandler(AdvancedGameNewsSettings gameNewsSettings)
    {
        _gameNewsSettings = gameNewsSettings;
    }
    public bool IsEditedGameNewsRelevant(EditedGameNewsRecord newsRecord)
    {
        throw new NotImplementedException();
    }

    public bool IsNewGameNewsRelevant(NewGameNewsRecord newsRecord)
    {
        throw new NotImplementedException();
    }

    public bool IsReleasedGameNewsRelevant(ReleaseGameNewsRecord newsRecord)
    {
        throw new NotImplementedException();
    }

    public bool IsScoreGameNewsRelevant(ScoreGameNewsRecord newsRecord)
    {
        throw new NotImplementedException();
    }
}
