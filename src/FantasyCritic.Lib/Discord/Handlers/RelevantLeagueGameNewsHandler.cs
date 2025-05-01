using FantasyCritic.Lib.Discord.Entity;
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Interfaces;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Discord.Models.GameNewsRecords;
using Serilog;


namespace FantasyCritic.Lib.Discord.Handlers;
public class RelevantLeagueGameNewsHandler : IRelevantGameNewsHandler
{
    private static readonly ILogger Logger = Log.ForContext<RelevantLeagueGameNewsHandler>();
    private LeagueYear _currentLeagueYear;
    private readonly IReadOnlyList<LeagueYear> _activeLeagueYears;
    private bool _showEligibleGameNewsOnly;
    private bool _showCurrentYearGameNewsOnly;
    private NotableMissSetting _notableMissSetting;
    private GameNewsSettings _newsSettings;
    private DiscordChannelKey _channelKey;
    public RelevantLeagueGameNewsHandler(LeagueChannelEntity leagueChannelEntity)
    {
        _currentLeagueYear = leagueChannelEntity.CurrentYear;
        _notableMissSetting = leagueChannelEntity.LeagueGameNewsSettings.NotableMissSetting;
        _newsSettings = leagueChannelEntity.GameNewsSettings;
        _activeLeagueYears = leagueChannelEntity.ActiveLeagueYears;
        _showCurrentYearGameNewsOnly = leagueChannelEntity.LeagueGameNewsSettings.ShowCurrentYearGameNewsOnly;
        _channelKey = leagueChannelEntity.ChannelKey;
        _showEligibleGameNewsOnly = leagueChannelEntity.LeagueGameNewsSettings.ShowEligibleGameNewsOnly;
    }

    public bool IsNewGameNewsRelevant(NewGameNewsRecord newsRecord)
    {

        MasterGame masterGame = newsRecord.MasterGame;
        LocalDate currentDate = newsRecord.CurrentDate;

        //Exit Early if the user has disabled new game news for this channel
        if (_newsSettings.ShowNewGameNews == false)
        {
            return false;
        }

        //Common Relevance Logic
        bool commonRelevance = CheckCommonLeagueRelevance(newsRecord);
        if (commonRelevance == true)
        {
            return true;
        }

        //Specific New Game Relevance Logic
        bool specificRelevance = false;

        return specificRelevance;
    }
    public bool IsEditedGameNewsRelevant(EditedGameNewsRecord newsRecord)
    {
        MasterGame masterGame = newsRecord.MasterGame;
        LocalDate currentDate = newsRecord.CurrentDate;

        //Exit Early if the user has disabled game edit news for this channel
        if (_newsSettings.ShowEditedGameNews == false)
        {
            return false;
        }

        //Common Relevance Logic
        bool commonRelevance = CheckCommonLeagueRelevance(newsRecord);
        if (commonRelevance == true)
        {
            return true;
        }

        //Specific Edited Game Relevance Logic
        bool specificRelevance = false;

        if(specificRelevance == true)
        {
            return true;
        }

        //Fallback
        Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, _channelKey);
        return false;

    }
    public bool IsReleasedGameNewsRelevant(ReleaseGameNewsRecord newsRecord)
    {
        MasterGame masterGame = newsRecord.MasterGame;
        LocalDate currentDate = newsRecord.CurrentDate;

        //Exit Early if the user has disabled Released game news for this channel
        if (_newsSettings.ShowReleasedGameNews == false)
        {
            return false;
        }

        //Common Relevance Logic
        bool commonRelevance = CheckCommonLeagueRelevance(newsRecord);
        if (commonRelevance == true)
        {
            return true;
        }

        //Specific Edited Game Relevance Logic
        bool specificRelevance = false;

        if (specificRelevance == true)
        {
            return true;
        }

        //Fallback
        Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, _channelKey);
        return false;
    }
    public bool IsScoreGameNewsRelevant(ScoreGameNewsRecord newsRecord)
    {
        MasterGame masterGame = newsRecord.MasterGame;
        LocalDate currentDate = newsRecord.CurrentDate;
        bool initialScore = newsRecord.OldScore == null;

        //Exit Early if the user has disabled score game news for this channel
        if (_newsSettings.ShowScoreGameNews == false)
        {
            return false;
        }

        //Common Relevance Logic
        bool commonRelevance = CheckCommonLeagueRelevance(newsRecord);
        if (commonRelevance == true)
        {
            return true;
        }

        //Specific Score News Relevance Logic
        bool specificRelevance = false;

        bool isNotableMiss = newsRecord.MasterGame.IsReleased(currentDate)
            && masterGame.HasAnyReviews
            && masterGame.CriticScore >= NotableMissSetting.Threshold;

        //If the game is a notable miss, check if the user wants to see it
        if (isNotableMiss)
        {
            specificRelevance =  CheckNotableMissRelevance(newsRecord, initialScore);
        }

        if (specificRelevance == true)
        {
            return true;
        }

        //Fallback
        Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, _channelKey);
        return false;
    }
    private bool CheckCommonLeagueRelevance(IGameNewsRecord newsRecord)
    {
        MasterGame masterGame = newsRecord.MasterGame;
        LocalDate currentDate = newsRecord.CurrentDate;



        //If all settings are turned on of course the league wants to see the new game update
        if (_newsSettings.AllGameUpdatesEnabled)
        {
            return true;
        }

        //If the game has any skipped tags dont show it!
        if (masterGame.Tags.Intersect(_newsSettings.SkippedTags).Any())
        {
            return false;
        }

        //Now check the years in the league and compare the news settings 
        foreach (var leagueYear in _activeLeagueYears)
        {
            bool inPublisherRoster = leagueYear.Publishers.Any(x => x.MyMasterGames.Contains(masterGame));
            bool eligibleInYear = leagueYear.GameIsEligibleInAnySlot(masterGame, currentDate);

            //Disable any news from other years if that option is enabled by the user
            if(leagueYear != _currentLeagueYear && _showCurrentYearGameNewsOnly)
            {
                return false;
            }


            //If the game is in the publisher roster we always want to show it - unless we decide to make this a setting in the future
            if (inPublisherRoster)
            {
                return true;
            }

            //If the game is not eligible in the league year, and user requested to not show ineligible games, skip it
            if (!eligibleInYear && !_showEligibleGameNewsOnly)
            {
                return false;
            }

            //This will provide game news for any game that is slated to release in the leagues years
            if (_newsSettings.ShowWillReleaseInYearNews && masterGame.WillReleaseInYear(leagueYear.Year))
            {
                return true;
            }

            //This will provide game updates for any game that might release in the leagues years
            if (_newsSettings.ShowMightReleaseInYearNews && masterGame.MightReleaseInYear(leagueYear.Year))
            {
                return true;
            }
        }

        //Fallback
        Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, _channelKey);
        return false;
    }
    private bool CheckNotableMissRelevance(IGameNewsRecord newsRecord, bool initialScore)
    {
        MasterGame masterGame = newsRecord.MasterGame;
        LocalDate currentDate = newsRecord.CurrentDate;


        if (_notableMissSetting == NotableMissSetting.None)
        {
            return false;
        }
        else if (_notableMissSetting == NotableMissSetting.ScoreUpdates)
        {
            return true;
        }
        else if (_notableMissSetting == NotableMissSetting.InitialScore)
        {
            if (initialScore) return true;
            else return false;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(_notableMissSetting), _notableMissSetting, null);
        }
    }
}
