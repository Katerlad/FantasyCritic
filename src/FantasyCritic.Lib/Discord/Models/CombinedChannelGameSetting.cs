using FantasyCritic.Lib.Discord.Enums;
using Serilog;

namespace FantasyCritic.Lib.Discord.Models;
public class CombinedChannelGameSetting
{
    private static readonly ILogger Logger = Log.ForContext<CombinedChannelGameSetting>();

    private readonly bool _sendLeagueMasterGameUpdates;
    private readonly bool _sendNotableMisses;
    private readonly AdvancedGameNewsSettings _advancedGameNewsSettings;
    private readonly IReadOnlyList<MasterGameTag> _skippedTags;

    private const Decimal NOTABLE_MISS_THRESHOLD = 83m;

    public CombinedChannelGameSetting(AdvancedGameNewsSettings advancedGameNewsSettings, IReadOnlyList<MasterGameTag> skippedTags)
    {
        _advancedGameNewsSettings = advancedGameNewsSettings;
        _skippedTags = skippedTags;
    }

    

    public bool NewGameIsRelevant(MasterGame masterGame, IReadOnlyList<LeagueYear>? activeLeagueYears, DiscordChannelKey channelKey, LocalDate currentDate)
    {
        //If all updates are off, we don't want to send any game updates
        if (_advancedGameNewsSettings.AllGameUpdatesEnabled == false)
        {
            return false;
        }

        //Skips specified tags setup in the channel settings.
        if (masterGame.Tags.Intersect(_skippedTags).Any())
        {
            return false;
        }

        //If this is a league channel it will have active years/ This logic will only show games that fall into the league
        if (_advancedGameNewsSettings.LeagueGameNewsEnabled && activeLeagueYears is not null)
        {
            foreach (var leagueYear in activeLeagueYears)
            {
                bool eligible = leagueYear.GameIsEligibleInAnySlot(masterGame, currentDate);
                if (!eligible)
                {
                    continue;
                }

                if (_advancedGameNewsSettings.WillReleaseInYearEnabled == true)
                {
                    return masterGame.WillReleaseInYear(leagueYear.Year);
                }
                if (_advancedGameNewsSettings.MightReleaseInYearEnabled == true)
                {
                    return masterGame.MightReleaseInYear(leagueYear.Year);
                }
            }
            // If we are here, it means the game is not eligible in any league year
            return false;
        }

       
        //These checks are for GameNews Only channels
        if (_advancedGameNewsSettings.WillReleaseInYearEnabled == true)
        {
            return masterGame.WillReleaseInYear(currentDate.Year);
        }

        if (_advancedGameNewsSettings.MightReleaseInYearEnabled == true)
        {
            return masterGame.MightReleaseInYear(currentDate.Year);
        }

        Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, channelKey);
        return false; // Default return value

    }


    public bool EditedGameIsRelevant(MasterGame masterGame, bool releaseStatusChanged, IReadOnlyList<LeagueYear>? activeLeagueYears,
        DiscordChannelKey channelKey, LocalDate currentDate)
    {
        //If all game updates are on we for sure want to send updates
        if (_advancedGameNewsSettings.AllGameUpdatesEnabled == true)
        {
            return true;
        }
        //if all game updates are off we don't want to send any game updates
        else if (_advancedGameNewsSettings.AllGameUpdatesEnabled == false)
        {
            return false;
        }

        //If combined channel has active league years we are dealing with a League Channel
        if (activeLeagueYears is not null)
        {
            foreach (var leagueYear in activeLeagueYears)
            {
                //See if game is part of a league year and asigned to a publisher
                bool claimedByPublisher = leagueYear.Publishers.Any(x => x.MyMasterGames.Contains(masterGame));

                //If the league channel wants master game updates and a publisher is assigned to the game, we want to send updates no matter what
                if (_advancedGameNewsSettings.LeagueGameNewsEnabled == true  && claimedByPublisher)
                {
                    return true;
                }

                //If League Game News is off, and the game is not assigned to a publisher, we don't want to send updates
                if (_advancedGameNewsSettings.LeagueGameNewsEnabled == false)
                {
                    return false;
                }

                //Logic going forward is for games without publishers in the league year, and Game News is on for the league channel
                //Do not update channel if game has a tag that user defined to skip
                if (masterGame.Tags.Intersect(_skippedTags).Any())
                {
                    continue;
                }

                //Check if game is eligible in the league year
                bool eligible = leagueYear.GameIsEligibleInAnySlot(masterGame, currentDate);
                if (!eligible)
                {
                    continue;
                }
                //If game has a score, and notable misses is turned off we don't want to send edit updates
                //Edits on realeased games with scores will probably be irrelevant to most users
                if (_advancedGameNewsSettings.NotableMissSetting == NotableMissesSetting.None && masterGame.HasAnyReviews)
                {
                    return false;
                }
                //If users have notable misses set to all they probably want all edits to games above the notable miss threshold
                if (_advancedGameNewsSettings.NotableMissSetting == NotableMissesSetting.All && masterGame.HasAnyReviews && masterGame.CriticScore >= NOTABLE_MISS_THRESHOLD)
                {
                    return true;
                }

                //Check Game News Settings for if the user wants will or might release updates
                //- This will always update the the channel if the games release status has changed even if these settings are off.
                //not sure if thats expected behaviour but thats how I inturpet it - Katerlad
                bool willReleaseRelevance = _advancedGameNewsSettings.WillReleaseInYearEnabled == true && masterGame.WillReleaseInYear(leagueYear.Year);
                bool mightReleaseRelevance = _advancedGameNewsSettings.MightReleaseInYearEnabled == true && masterGame.MightReleaseInYear(leagueYear.Year);
                bool releaseRelevance = releaseStatusChanged || willReleaseRelevance || mightReleaseRelevance;
                if (releaseRelevance)
                {
                    return true;
                }
            }


            //We shouldnt get here but a fallback just in case
            return false;
        }

        //This logic below would be for GameNews Only Channels
        if (masterGame.Tags.Intersect(_skippedTags).Any())
        {
            return false;
        }
        if (_advancedGameNewsSettings.WillReleaseInYearEnabled == true)
        {
            return masterGame.WillReleaseInYear(currentDate.Year);
        }
        if (_advancedGameNewsSettings.MightReleaseInYearEnabled == true)
        {
            return masterGame.MightReleaseInYear(currentDate.Year);
        }
        


        Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, channelKey);
        return false;
    }

    public bool ReleasedGameIsRelevant(MasterGame masterGame, IReadOnlyList<LeagueYear>? activeLeagueYears, DiscordChannelKey discordChannelKey)
    {
        if (_advancedGameNewsSettings.AllGameUpdatesEnabled == true)
        {
            return true;
        }

        //If combined channel has active league years we are dealing with a League Channel
        //League Channels only get Released Game Updates if League Game News is enabled
        if (_advancedGameNewsSettings.LeagueGameNewsEnabled && activeLeagueYears is not null)
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

        if (masterGame.Tags.Intersect(_skippedTags).Any())
        {
            return false;
        }
        if (_advancedGameNewsSettings.NotableMissSetting == NotableMissesSetting.None && masterGame.HasAnyReviews)
        {
            return false;
        }

        //If all game updates are disabled, we don't want to send any game updates
        return !_advancedGameNewsSettings.AllGameUpdatesEnabled == false;
    }

    public bool ScoredGameIsRelevant(MasterGame masterGame, IReadOnlyList<LeagueYear>? activeLeagueYears, decimal? criticScore,decimal? oldScore, LocalDate currentDate)
    {
        //Dont need to look any further if all game updates are enabled
        if (_advancedGameNewsSettings.AllGameUpdatesEnabled)
        {
            return true;
        }

        //Check if game is notable miss
        bool isNotableMiss = criticScore is >= NOTABLE_MISS_THRESHOLD && masterGame.HasAnyReviews;

        //if combined channel has active league years we are dealing with a League Channel
        //however the league channel needs to enable master game updates to get score changes
        if (_advancedGameNewsSettings.LeagueGameNewsEnabled && activeLeagueYears is not null)
        {
            foreach (var leagueYear in activeLeagueYears)
            {
                bool isClaimedByPublisher = leagueYear.Publishers.Any(x => x.MyMasterGames.Contains(masterGame));
                //Always update scores if the game is claimed by a publisher
                if (isClaimedByPublisher)
                {
                    return true;
                }

                
                if (_sendNotableMisses && criticScore is >= NOTABLE_MISS_THRESHOLD && leagueYear.GameIsEligibleInAnySlot(masterGame, currentDate))
                {
                    return true;
                }
            }

            return false;
        }

        
        if (_advancedGameNewsSettings.NotableMissSetting == NotableMissesSetting.None)
        {
            return false;
        }
        //If there is a previous score, and the user only wants initial score updates, we don't want to send any game updates
        if (_advancedGameNewsSettings.NotableMissSetting == NotableMissesSetting.InitialScore && oldScore is not null)
        {
            return false;
        }
        //If the user wants all notable miss updates, we want to send all score updates above the threshold
        if (_advancedGameNewsSettings.NotableMissSetting == NotableMissesSetting.All && criticScore >= NOTABLE_MISS_THRESHOLD)
        {
            return true;
        }

        if (masterGame.Tags.Intersect(_skippedTags).Any())
        {
            return false;
        }

        return !_advancedGameNewsSettings.AllGameUpdatesEnabled == false;
    }

    public override string ToString()
    {
        List<string> parts = new List<string>();
        if (_advancedGameNewsSettings.AllGameUpdatesEnabled)
        {
            parts.Add("All Master Game Updates");
        }
        else
        {
            parts.Add("No Game Updates");
        }
        if (_advancedGameNewsSettings.LeagueGameNewsEnabled)
        {
            parts.Add("League Master Game Updates");
        }
        else
        {
            parts.Add("No League Master Game Updates");
        }

        if (_advancedGameNewsSettings.AllNonLeagueGameUpdatesEnabled)
        {
            parts.Add("All Non-League Master Game Updates");
            
        }
        else
        {
            parts.Add("No Non-League Master Game Updates");
        }
        if (_advancedGameNewsSettings.MightReleaseInYearEnabled)
        {
            parts.Add("Any 'Might Release' Master Game Updates");
        }
        else
        {
            parts.Add("No 'Might Release' Master Game Updates");
        }
        if (_advancedGameNewsSettings.WillReleaseInYearEnabled)
        {
            parts.Add("Any 'Will Release' Master Game Updates");
        }
        else
        {
            parts.Add("No 'Will Release' Master Game Updates");
        }

        return string.Join(',', parts);
    }
}
