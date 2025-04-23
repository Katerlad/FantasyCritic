using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Discord.Models.GameNewsRecords;
using FantasyCritic.Lib.Domain;


namespace FantasyCritic.Lib.Discord.Handlers;
internal class RelevantLeagueGameNewsHandler : IRelevantGameNewsHandler
{
    private readonly IReadOnlyList<LeagueYear> _activeLeagueYears;
    private bool _leagueGameNewsEnabled;
    private NotableMissesSetting _notableMissesSetting;
    private AdvancedGameNewsSettings _newsSettings;
    public RelevantLeagueGameNewsHandler(AdvancedGameNewsSettings gameNewsSettings,bool leagueGameNewsEnabled, NotableMissesSetting notableMissesSetting, IReadOnlyList<LeagueYear> leagueYears)
    {
        _leagueGameNewsEnabled = leagueGameNewsEnabled;
        _notableMissesSetting = notableMissesSetting;
        _newsSettings = gameNewsSettings;
        _activeLeagueYears = leagueYears;
    }

    public bool IsNewGameNewsRelevant(NewGameNewsRecord newsRecord)
    {

        MasterGame masterGame = newsRecord.masterGame;
        LocalDate currentDate = newsRecord.currentDate;


        //The User has requested no game news be shown to their league channel
        if (!_leagueGameNewsEnabled)
        {
            return false;
        }

        //If all settings are turned on of course the league wants to see the new game update
        if (_newsSettings.AllGameUpdatesEnabled)
        {
            return true;
        }

        //If the game has any skipped tags dont show it!
        if (newsRecord.masterGame.Tags.Intersect(_newsSettings.SkippedTags).Any())
        {
            return false;
        }

        //Now check the years in the league and compare the news settings 
        foreach (var leagueYear in _activeLeagueYears)
        {
            //This will provide game news for any game that is slated to release in the leagues years
            if (_newsSettings.WillReleaseInYearEnabled && masterGame.WillReleaseInYear(leagueYear.Year))
            {
                return true;
            }

            //This will provide game updates for any game that might release in the leagues years
            if (_newsSettings.MightReleaseInYearEnabled && masterGame.MightReleaseInYear(leagueYear.Year))
            {
                return true;
            }
        }

        //Fallback
        return false;
    }
    public bool IsEditedGameNewsRelevant(EditedGameNewsRecord newsRecord)
    {
        throw new NotImplementedException();
    }

    

    public bool IsReleasedGameNewsRelevant(ReleaseGameNewsRecord newsRecord)
    {
        //If combined channel has active league years we are dealing with a League Channel
        //League Channels only get Released Game Updates if League Game News is enabled
        if (activeLeagueYears is not null)
        {
            foreach (var leagueYear in activeLeagueYears)
            {
                bool isClaimedByPublisher = leagueYear.Publishers.Any(x => x.MyMasterGames.Contains(masterGame));

                //We always want to send updates of a game if a publisher is assigned to it
                if (isClaimedByPublisher)
                {
                    return true;
                }

                //We might want to skip tags in the future
                if (masterGame.Tags.Intersect(_skippedTags).Any())
                {
                    continue;
                }
            }
            //Fallback
            Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, discordChannelKey);
            return false;
        }
    }

    public bool IsScoreGameNewsRelevant(ScoreGameNewsRecord newsRecord)
    {
        throw new NotImplementedException();
    }
}
