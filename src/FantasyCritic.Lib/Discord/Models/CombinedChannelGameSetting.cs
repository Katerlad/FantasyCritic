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

        if (_advancedGameNewsSettings.AllGameUpdatedEnabled == true)
        {
            return true;
        }

        if (masterGame.Tags.Intersect(_skippedTags).Any())
        {
            return false;
        }

        if (activeLeagueYears is not null)
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
        if (_advancedGameNewsSettings.AllGameUpdatedEnabled == true)
        {
            return true;
        }

        if (activeLeagueYears is not null)
        {
            foreach (var leagueYear in activeLeagueYears)
            {
                bool inLeagueYear = leagueYear.Publishers.Any(x => x.MyMasterGames.Contains(masterGame));
                if (_advancedGameNewsSettings.LeagueGameNewsEnabled == true  && inLeagueYear)
                {
                    return true;
                }

                if (_advancedGameNewsSettings.AllGameUpdatedEnabled == false)
                {
                    continue;
                }

                if (masterGame.Tags.Intersect(_skippedTags).Any())
                {
                    continue;
                }

                bool eligible = leagueYear.GameIsEligibleInAnySlot(masterGame, currentDate);
                if (!eligible)
                {
                    continue;
                }

                bool willReleaseRelevance = _advancedGameNewsSettings.WillReleaseInYearEnabled == true && masterGame.WillReleaseInYear(leagueYear.Year);
                bool mightReleaseRelevance = _advancedGameNewsSettings.MightReleaseInYearEnabled == true && masterGame.MightReleaseInYear(leagueYear.Year);
                bool releaseRelevance = releaseStatusChanged || willReleaseRelevance || mightReleaseRelevance;
                if (releaseRelevance)
                {
                    return true;
                }

                if (_advancedGameNewsSettings.UnannouncedGameNewsEnabled == true )
                {
                    return true;
                }
            }

            return false;
        }

        
        if (_advancedGameNewsSettings.AllGameUpdatedEnabled == false)
        {
            return false;
        }
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
        if (_advancedGameNewsSettings.NotableMissSetting == NotableMissesSetting.None && masterGame.HasAnyReviews)
        {
            return false;
        }


        Logger.Warning("Invalid game news configuration for: {gameName}, {channelKey}", masterGame.GameName, channelKey);
        return false;
    }

    public bool ReleasedGameIsRelevant(MasterGame masterGame, IReadOnlyList<LeagueYear>? activeLeagueYears)
    {
        if (_advancedGameNewsSettings.AllGameUpdatedEnabled == true)
        {
            return true;
        }

        if (_sendLeagueMasterGameUpdates && activeLeagueYears is not null)
        {
            foreach (var leagueYear in activeLeagueYears)
            {
                bool inLeagueYear = leagueYear.Publishers.Any(x => x.MyMasterGames.Contains(masterGame));
                if (inLeagueYear)
                {
                    return true;
                }
            }

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
        return !_advancedGameNewsSettings.AllGameUpdatedEnabled == false;
    }

    public bool ScoredGameIsRelevant(MasterGame masterGame, IReadOnlyList<LeagueYear>? activeLeagueYears, decimal? criticScore,decimal? oldScore, LocalDate currentDate)
    {
        if (_advancedGameNewsSettings.AllGameUpdatedEnabled)
        {
            return true;
        }

        bool isNotableMiss = criticScore is >= NOTABLE_MISS_THRESHOLD && masterGame.HasAnyReviews;

        if (_sendLeagueMasterGameUpdates && activeLeagueYears is not null)
        {
            foreach (var leagueYear in activeLeagueYears)
            {
                bool inLeagueYear = leagueYear.Publishers.Any(x => x.MyMasterGames.Contains(masterGame));
                if (inLeagueYear)
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

        return !_advancedGameNewsSettings.AllGameUpdatedEnabled == false;
    }

    public override string ToString()
    {
        List<string> parts = new List<string>();
        if (_advancedGameNewsSettings.AllGameUpdatedEnabled)
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
