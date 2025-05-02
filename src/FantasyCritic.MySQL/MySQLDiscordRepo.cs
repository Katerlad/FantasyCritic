using FantasyCritic.Lib.DependencyInjection;
using FantasyCritic.Lib.Discord.Entity;
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Domain.Conferences;
using FantasyCritic.Lib.Extensions;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.MySQL.Entities;
using FantasyCritic.MySQL.Entities.Discord;
using Serilog;
using System.Data;

namespace FantasyCritic.MySQL;

public class MySQLDiscordRepo : IDiscordRepo
{
    private readonly IFantasyCriticRepo _fantasyCriticRepo;
    private readonly IMasterGameRepo _masterGameRepo;
    private readonly IConferenceRepo _conferenceRepo;
    private readonly ICombinedDataRepo _combinedDataRepo;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    private readonly string _connectionString;

    public MySQLDiscordRepo(RepositoryConfiguration configuration,
        IFantasyCriticRepo fantasyCriticRepo, IMasterGameRepo masterGameRepo, IConferenceRepo conferenceRepo, ICombinedDataRepo combinedDataRepo,
        IClock clock)
    {
        _fantasyCriticRepo = fantasyCriticRepo;
        _masterGameRepo = masterGameRepo;
        _conferenceRepo = conferenceRepo;
        _combinedDataRepo = combinedDataRepo;
        _clock = clock;
        _logger = Serilog.Log.ForContext<MySQLDiscordRepo>();
        _connectionString = configuration.ConnectionString;
    }

    public async Task SetLeagueChannel(Guid leagueID, ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var minimalLeagueChannelRecord = new MinimalLeagueChannelRecord(guildID, channelID, leagueID, null);
        var existingChannel = await GetLeagueChannelEntity(guildID, channelID);
        var existingChannelMinimal = existingChannel?.ToMinimalDomain();
        var sql = existingChannel == null
            ? "INSERT INTO tbl_discord_leaguechannel (GuildID,ChannelID,LeagueID) VALUES (@GuildID, @ChannelID, @LeagueID)"
            : "UPDATE tbl_discord_leaguechannel SET LeagueID=@LeagueID WHERE ChannelID=@ChannelID AND GuildID=@GuildID";
        var entity = existingChannel == null
            ? minimalLeagueChannelRecord
            : existingChannelMinimal;
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task SetConferenceChannel(Guid conferenceID, ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var conferenceChannelEntity = new ConferenceChannelEntity(guildID, channelID, conferenceID);
        var existingChannel = await GetConferenceChannelEntity(guildID, channelID);
        var sql = existingChannel == null
            ? "INSERT INTO tbl_discord_conferencechannel (GuildID,ChannelID,ConferenceID) VALUES (@GuildID, @ChannelID, @ConferenceID)"
            : "UPDATE tbl_discord_conferencechannel SET ConferenceID=@ConferenceID WHERE ChannelID=@ChannelID AND GuildID=@GuildID";
        await connection.ExecuteAsync(sql, conferenceChannelEntity);
    }

    public async Task SetLeagueGameNewsSetting(Guid leagueID, ulong guildID, ulong channelID, LeagueGameNewsSettingsRecord leagueGameNewsSettings)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var leagueGameNewsEntity = new LeagueGameNewsSettingsEntity(
            guildID,
            channelID,
            leagueID,
            leagueGameNewsSettings.ShowPickedGameNews,
            leagueGameNewsSettings.ShowEligibleGameNews,
            leagueGameNewsSettings.ShowCurrentYearGameNewsOnly,
            leagueGameNewsSettings.NotableMissSetting);
        var sql = @"
            INSERT INTO tbl_discord_league_gamenewsoptions 
            (LeagueID, GuildID, ChannelID, NotableMissSetting, ShowPickedGameNews, ShowEligibleGameNews, ShowCurrentYearGameNewsOnly)
            VALUES 
            (@LeagueID, @GuildID, @ChannelID, @NotableMissSetting, @ShowPickedGameNews, @ShowEligibleGameNews, @ShowCurrentYearGameNewsOnly)
            ON DUPLICATE KEY UPDATE 
            NotableMissSetting = @NotableMissSetting,
            ShowPickedGameNews = @ShowPickedGameNews,
            ShowEligibleGameNews = @ShowEligibleGameNews,
            ShowCurrentYearGameNewsOnly = @ShowCurrentYearGameNewsOnly";
        await connection.ExecuteAsync(sql, leagueGameNewsEntity);
    }

    public async Task SetGameNewsSetting(ulong guildID, ulong channelID, GameNewsSettingsRecord gameNewsSettings)
    {
        var deleteChannelSQL = "DELETE FROM tbl_discord_gamenewschannel WHERE GuildID=@GuildID AND ChannelID=@ChannelID;";
        var deleteOptionsSQL = "DELETE FROM tbl_discord_gamenewsoptions WHERE GuildID=@GuildID AND ChannelID=@ChannelID;";
        var insertChannelSQL = "INSERT INTO tbl_discord_gamenewschannel (GuildID, ChannelID) VALUES (@GuildID, @ChannelID);";

        var insertOptionsSQL = @"
        REPLACE INTO tbl_discord_gamenewsoptions (
            GuildID, ChannelID, EnableGameNews,
            ShowMightReleaseInYearNews,
            ShowWillReleaseInYearNews,
            ShowScoreGameNews,
            ShowReleasedGameNews,
            ShowNewGameNews,
            ShowEditedGameNews
        ) VALUES (
            @GuildID, @ChannelID, @EnableGameNews,
            @ShowMightReleaseInYearNews,
            @ShowWillReleaseInYearNews,
            @ShowScoreGameNews,
            @ShowReleasedGameNews,
            @ShowNewGameNews,
            @ShowEditedGameNews
        );";

        var selectTagsSQL = "SELECT * FROM tbl_discord_gamenewschannelskiptag WHERE GuildID=@GuildID AND ChannelID=@ChannelID;";
        var deleteTagsSQL = "DELETE FROM tbl_discord_gamenewschannelskiptag WHERE GuildID=@GuildID AND ChannelID=@ChannelID;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var param = new
        {
            GuildID = guildID,
            ChannelID = channelID
        };

        var masterGameTagEntities = (await connection.QueryAsync<GameNewsChannelSkippedTagEntity>(selectTagsSQL, param)).ToList();

        await using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(deleteTagsSQL, param, transaction);
        await connection.ExecuteAsync(deleteChannelSQL, param, transaction);
        await connection.ExecuteAsync(deleteOptionsSQL, param, transaction);

        // Insert channel row
        await connection.ExecuteAsync(insertChannelSQL, param, transaction);

        // Insert or update options row
        var optionsParam = new
        {
            GuildID = guildID,
            ChannelID = channelID,
            gameNewsSettings.EnableGameNews,
            gameNewsSettings.ShowMightReleaseInYearNews,
            gameNewsSettings.ShowWillReleaseInYearNews,
            gameNewsSettings.ShowScoreGameNews,
            gameNewsSettings.ShowReleasedGameNews,
            gameNewsSettings.ShowNewGameNews,
            gameNewsSettings.ShowEditedGameNews
        };

        await connection.ExecuteAsync(insertOptionsSQL, optionsParam, transaction);

        await transaction.CommitAsync();

        // Re-insert tag skips
        await SetSkippedGameNewsTags(guildID, channelID, gameNewsSettings.SkippedTags);

        
    }

    public async Task SetSkippedGameNewsTags(ulong guildID, ulong channelID, IEnumerable<MasterGameTag> skippedTags)
    {
        var param = new
        {
            guildID,
            channelID
        };
        const string deleteTagsSQL = "delete from tbl_discord_gamenewschannelskiptag where GuildID=@guildID AND ChannelID=@channelID;";

        var tagEntities = skippedTags.Select(x => new GameNewsChannelSkippedTagEntity(guildID, channelID, x));

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(deleteTagsSQL, param, transaction);
        await connection.BulkInsertAsync<GameNewsChannelSkippedTagEntity>(tagEntities, "tbl_discord_gamenewschannelskiptag", 500, transaction);
        await transaction.CommitAsync();
    }

    public async Task SetBidAlertRoleId(Guid leagueID, ulong guildID, ulong channelID, ulong? bidAlertRoleID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var minimalLeagueChannelRecord = new MinimalLeagueChannelRecord(guildID, channelID, leagueID, bidAlertRoleID);
        var sql = "UPDATE tbl_discord_leaguechannel SET BidAlertRoleID=@BidAlertRoleID WHERE LeagueID=@LeagueID AND GuildID=@GuildID AND ChannelID=@ChannelID";
        await connection.ExecuteAsync(sql, minimalLeagueChannelRecord);
    }

    public async Task<bool> DeleteLeagueChannel(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            guildID,
            channelID
        };
        var sql = "DELETE FROM tbl_discord_leaguechannel WHERE GuildID=@guildID AND ChannelID=@channelID";
        var rowsDeleted = await connection.ExecuteAsync(sql, queryObject);
        return rowsDeleted >= 1;
    }

    public async Task<bool> DeleteConferenceChannel(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            guildID,
            channelID
        };
        var sql = "DELETE FROM tbl_discord_conferencechannel WHERE GuildID=@guildID AND ChannelID=@channelID";
        var rowsDeleted = await connection.ExecuteAsync(sql, queryObject);
        return rowsDeleted >= 1;
    }

    public async Task<bool> DeleteGameNewsChannel(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            guildID,
            channelID
        };
        var sql = "DELETE FROM tbl_discord_gamenewschannel WHERE GuildID=@guildID AND ChannelID=@channelID";
        var rowsDeleted = await connection.ExecuteAsync(sql, queryObject);
        return rowsDeleted >= 1;
    }

    public async Task<IReadOnlyList<MinimalLeagueChannelRecord>> GetAllMinimalLeagueChannels()
    {
        await using var connection = new MySqlConnection(_connectionString);
        const string sql = "select * from tbl_discord_leaguechannel";

        var leagueChannels = await connection.QueryAsync<MinimalLeagueChannelRecord>(sql);
        return leagueChannels.ToList();
    }

    public async Task<IReadOnlyList<LeagueChannelRecord>> GetAllLeagueChannels()
    {
        var minimalLeagueChannels = await GetAllMinimalLeagueChannels();

        var leagueChannelIds = minimalLeagueChannels.Select(x => x.LeagueID).Distinct().ToList();

        var leagueChannelEntities = new List<LeagueChannelRecord>();

        foreach (var leagueID in leagueChannelIds)
        {
            var channels = await GetLeagueChannels(leagueID);
            leagueChannelEntities.AddRange(channels);
        }

        return leagueChannelEntities;
    }

    public async Task<IReadOnlyList<GameNewsOnlyChannelRecord>> GetAllGameNewsChannels()
    {
        var possibleTags = await _masterGameRepo.GetMasterGameTags();

        await using var connection = new MySqlConnection(_connectionString);
        const string channelSQL = "select * from tbl_discord_gamenewschannel";
        const string tagSQL = "select * from tbl_discord_gamenewschannelskiptag";
        const string optionsSQL = "select * from tbl_discord_gamenewsoptions";

        var channelEntities = await connection.QueryAsync<GameNewsChannelEntity>(channelSQL);
        var tagEntities = await connection.QueryAsync<GameNewsChannelSkippedTagEntity>(tagSQL);
        var gameNewsoptionEntities = await connection.QueryAsync<GameNewsOptionsEntity>(optionsSQL);

        var tagLookup = tagEntities.ToLookup(x => new DiscordChannelKey(x.GuildID, x.ChannelID));
        var gameNewsSettingsLookup = gameNewsoptionEntities.ToLookup(x => new DiscordChannelKey(x.GuildID, x.ChannelID));

        List<GameNewsOnlyChannelRecord> gameNewsChannels = new List<GameNewsOnlyChannelRecord>();
        foreach (var channelEntity in channelEntities)
        {
            var tagAssociations = tagLookup[new DiscordChannelKey(channelEntity.GuildID, channelEntity.ChannelID)].Select(x => x.TagName).ToList();
            IReadOnlyList<MasterGameTag> tags = possibleTags
                .Where(x => tagAssociations.Contains(x.Name))
                .ToList();

            var gameNewsSettingsEntity = gameNewsSettingsLookup[new DiscordChannelKey(channelEntity.GuildID, channelEntity.ChannelID)].FirstOrDefault();
            GameNewsSettingsRecord? gameNewsSettings = null;

            if (gameNewsSettingsEntity != null)
            {
                gameNewsSettings = new GameNewsSettingsRecord
                {
                    EnableGameNews = gameNewsSettingsEntity.EnableGameNews,
                    ShowMightReleaseInYearNews = gameNewsSettingsEntity.ShowMightReleaseInYearNews,
                    ShowWillReleaseInYearNews = gameNewsSettingsEntity.ShowWillReleaseInYearNews,
                    ShowScoreGameNews = gameNewsSettingsEntity.ShowScoreGameNews,
                    ShowReleasedGameNews = gameNewsSettingsEntity.ShowReleasedGameNews,
                    ShowNewGameNews = gameNewsSettingsEntity.ShowNewGameNews,
                    ShowEditedGameNews = gameNewsSettingsEntity.ShowEditedGameNews,
                    SkippedTags = tags.ToList()
                };
            }

            gameNewsChannels.Add(channelEntity.ToDomain(tags,gameNewsSettings));
        }

        return gameNewsChannels;
    }

    public async Task<IReadOnlyList<LeagueChannelRecord>> GetLeagueChannels(Guid leagueID)
    {
        await using var connection = new MySqlConnection(_connectionString);

        // Query all League Channels for the given LeagueID
        const string leagueChannelSQL = @"
            SELECT GuildID, ChannelID, LeagueID, BidAlertRoleID 
            FROM tbl_discord_leaguechannel 
            WHERE LeagueID = @leagueID;";
        var minimalLeagueChannelRecords = (await connection.QueryAsync<MinimalLeagueChannelRecord>(leagueChannelSQL, new { leagueID })).ToList();

        if (!minimalLeagueChannelRecords.Any())
        {
            return new List<LeagueChannelRecord>(); // Return an empty list if no channels are found
        }

        // Use MySQLFantasyCriticRepo to get Active League Years for the LeagueID
        var activeLeagueYears = await _fantasyCriticRepo.GetActiveLeagueYears(new List<Guid> { leagueID });

        // Get the Current League Year
        var currentYear = activeLeagueYears.FirstOrDefault(x => x.Year == DateTime.UtcNow.Year);
        if (currentYear == null)
        {
            throw new InvalidOperationException("No current league year found for the league.");
        }

        // Prepare the list of LeagueChannelEntity objects
        var leagueChannelRecords = new List<LeagueChannelRecord>();

        foreach (var minimalRecord in minimalLeagueChannelRecords)
        {
            // Query the Game News Settings for each channel
            var gameNewsSettings = await GetGameNewsSettings(minimalRecord.GuildID, minimalRecord.ChannelID);


            // Query the League Game News Settings for each channel
            var leagueNewsSettings = await GetLeagueGameNewsSettings(minimalRecord.GuildID, minimalRecord.ChannelID);

            LeagueGameNewsSettingsRecord? leagueGameNewsSettingsRecord = null;


            // Create the LeagueChannelRecord and add it to the list
            var leagueChannelRecord = new LeagueChannelRecord(
                minimalRecord.GuildID,
                minimalRecord.ChannelID,
                minimalRecord.LeagueID,
                currentYear,
                activeLeagueYears,
                leagueGameNewsSettingsRecord,
                gameNewsSettings,
                minimalRecord.BidAlertRoleID);

            leagueChannelRecords.Add(leagueChannelRecord);
        }

        return leagueChannelRecords;
    }

    public async Task<IReadOnlyList<MinimalConferenceChannel>> GetConferenceChannels(Guid conferenceID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            conferenceID
        };

        const string conferenceChannelSQL = "select * from tbl_discord_conferencechannel WHERE ConferenceID = @conferenceID";

        var conferenceChannels = await connection.QueryAsync<ConferenceChannelEntity>(conferenceChannelSQL, queryObject);
        return conferenceChannels.Select(l => l.ToMinimalDomain()).ToList();
    }

    public async Task<LeagueChannelRecord?> GetLeagueChannel(ulong guildID, ulong channelID, IReadOnlyList<SupportedYear> supportedYears, int? year = null)
    {
        var leagueChannelEntity = await GetLeagueChannelEntity(guildID, channelID);
        if (leagueChannelEntity is null)
        {
            return null;
        }

        LeagueYear? leagueYear = null;

        if (year != null)
        {
            leagueYear = await _combinedDataRepo.GetLeagueYear(leagueChannelEntity.LeagueID, year.Value);
        }
        else
        {
            var league = await _fantasyCriticRepo.GetLeague(leagueChannelEntity.LeagueID);
            if (league is null)
            {
                return null;
            }
            var supportedYear = supportedYears
                .OrderBy(y => y.Year)
                .FirstOrDefault(y => !y.Finished && league.Years.Contains(y.Year));
            if (supportedYear == null)
            {
                return null;
            }

            leagueYear = await _combinedDataRepo.GetLeagueYear(leagueChannelEntity.LeagueID, supportedYear.Year);
        }

        return leagueYear is null
            ? null
            : leagueChannelEntity.ToDomain(leagueYear);
    }

    public async Task<ConferenceChannel?> GetConferenceChannel(ulong guildID, ulong channelID, IReadOnlyList<SupportedYear> supportedYears, int? year = null)
    {
        var conferenceChannelEntity = await GetConferenceChannelEntity(guildID, channelID);
        if (conferenceChannelEntity is null)
        {
            return null;
        }

        ConferenceYear? conferenceYear = null;

        if (year != null)
        {
            conferenceYear = await _conferenceRepo.GetConferenceYear(conferenceChannelEntity.ConferenceID, year.Value);
        }
        else
        {
            var conference = await _conferenceRepo.GetConference(conferenceChannelEntity.ConferenceID);
            if (conference is null)
            {
                return null;
            }
            var supportedYear = supportedYears
                .OrderBy(y => y.Year)
                .FirstOrDefault(y => !y.Finished && conference.Years.Contains(y.Year));
            if (supportedYear == null)
            {
                return null;
            }

            conferenceYear = await _conferenceRepo.GetConferenceYear(conferenceChannelEntity.ConferenceID, supportedYear.Year);
        }

        return conferenceYear is null
            ? null
            : conferenceChannelEntity.ToDomain(conferenceYear);
    }

    public async Task<GameNewsOnlyChannelRecord?> GetGameNewsChannel(ulong guildID, ulong channelID)
    {
        var possibleTags = await _masterGameRepo.GetMasterGameTags();

        await using var connection = new MySqlConnection(_connectionString);
        const string channelSQL = "SELECT * FROM tbl_discord_gamenewschannel WHERE GuildID = @guildID AND ChannelID = @channelID;";
        const string tagSQL = "SELECT * FROM tbl_discord_gamenewschannelskiptag WHERE GuildID = @guildID AND ChannelID = @channelID;";
        const string optionsSQL = "SELECT * FROM tbl_discord_gamenewsoptions WHERE GuildID = @guildID AND ChannelID = @channelID;";

        var queryObject = new
        {
            guildID,
            channelID
        };

        // Query the GameNewsChannel entity
        var entity = await connection.QuerySingleOrDefaultAsync<GameNewsChannelEntity>(channelSQL, queryObject);
        if (entity == null)
        {
            return null; // No GameNewsChannel found
        }

        // Query the associated tags
        var tagEntities = await connection.QueryAsync<GameNewsChannelSkippedTagEntity>(tagSQL, queryObject);
        var tagAssociations = tagEntities.Select(x => x.TagName).ToList();
        IReadOnlyList<MasterGameTag> tags = possibleTags
            .Where(x => tagAssociations.Contains(x.Name))
            .ToList();

        // Query the associated GameNewsOptions
        var optionsEntity = await connection.QuerySingleOrDefaultAsync<GameNewsOptionsEntity>(optionsSQL, queryObject);
        if (optionsEntity == null)
        {
            throw new InvalidOperationException($"GameNewsOptions not found for GuildID: {guildID}, ChannelID: {channelID}");
        }

        // Map the GameNewsOptions to GameNewsSettings
        var gameNewsSettings = new GameNewsSettingsRecord
        {
            ShowMightReleaseInYearNews = optionsEntity.ShowMightReleaseInYearNews,
            ShowWillReleaseInYearNews = optionsEntity.ShowWillReleaseInYearNews,
            ShowScoreGameNews = optionsEntity.ShowScoreGameNews,
            ShowReleasedGameNews = optionsEntity.ShowReleasedGameNews,
            ShowNewGameNews = optionsEntity.ShowNewGameNews,
            ShowEditedGameNews = optionsEntity.ShowEditedGameNews
        };

        // Return the combined result
        return new GameNewsOnlyChannelRecord(
            guildID,
            channelID,
            tags,
            gameNewsSettings
        );
    }

    public async Task<LeagueGameNewsSettingsRecord?> GetLeagueGameNewsSettings(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            GuildID = guildID,
            ChannelID = channelID
        };
        const string leagueGameNewsSQL = "SELECT * FROM tbl_discord_league_gamenewsoptions WHERE GuildID = @GuildID AND ChannelID = @ChannelID;";
        var leagueGameNewsSettingsEntity = await connection.QuerySingleOrDefaultAsync<LeagueGameNewsSettingsEntity>(leagueGameNewsSQL, queryObject);
        if (leagueGameNewsSettingsEntity == null)
        {
            return null; // No data found
        }
        // Map the result to LeagueGameNewsSettingsRecord
        return leagueGameNewsSettingsEntity.ToRecord();
    }

    public async Task<GameNewsSettingsRecord?> GetGameNewsSettings(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            GuildID = guildID,
            ChannelID = channelID
        };
        const string gameNewsSQL = "SELECT * FROM tbl_discord_gamenewsoptions WHERE GuildID = @GuildID AND ChannelID = @ChannelID;";const string skippedTagsSQL = "SELECT * FROM tbl_discord_gamenewschannelskiptag WHERE GuildID = @GuildID AND ChannelID = @ChannelID;";
        var gameNewsSettingsEntity = await connection.QuerySingleOrDefaultAsync<GameNewsOptionsEntity>(gameNewsSQL, queryObject);

        if (gameNewsSettingsEntity == null)
        {
            return null; // No data found
        }

        var skippedTagsEntities = await connection.QueryAsync<GameNewsChannelSkippedTagEntity>(skippedTagsSQL, queryObject);

        var skippedStringTags = skippedTagsEntities.Select(x => x.TagName).ToList();

        var masterGameTags = await _masterGameRepo.GetMasterGameTags();

        var tagsDictionary = masterGameTags.ToDictionary(x => x.Name, x => x);

        // Filter the skipped tags to only include those that exist in the master game tags
        var skippedTags = skippedStringTags
            .Where(tag => tagsDictionary.ContainsKey(tag))
            .Select(tag => tagsDictionary[tag])
            .ToList();

        // Map the result to GameNewsSettings
        return new GameNewsSettingsRecord
        {
            EnableGameNews = gameNewsSettingsEntity.EnableGameNews,
            ShowMightReleaseInYearNews = gameNewsSettingsEntity.ShowMightReleaseInYearNews,
            ShowWillReleaseInYearNews = gameNewsSettingsEntity.ShowWillReleaseInYearNews,
            ShowScoreGameNews = gameNewsSettingsEntity.ShowScoreGameNews,
            ShowReleasedGameNews = gameNewsSettingsEntity.ShowReleasedGameNews,
            ShowNewGameNews = gameNewsSettingsEntity.ShowNewGameNews,
            ShowEditedGameNews = gameNewsSettingsEntity.ShowEditedGameNews,
            SkippedTags = skippedTags
        };
    }

    public async Task<CompleteGameNewsSettings?> GetCompleteGameNewsSettings(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            GuildID = guildID,
            ChannelID = channelID
        };
        var tagsDictionary = await _masterGameRepo.GetMasterGameTagDictionary();

        var result = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "sp_get_complete_gamenews_settings",
            queryObject,
            commandType: CommandType.StoredProcedure);

        if (result == null)
        {
            return null; // No data found
        }

        // Map the result to GameNewsAdvancedSettings
        return new CompleteGameNewsSettings
        {
            //League Game News Settings
            ShowPickedGameNews = result.ShowPickedGameNews,
            ShowEligibleGameNews = result.ShowEligibleGameNews,
            ShowCurrentYearGameNewsOnly = result.ShowCurrentYearGameNewsOnly,
            NotableMissSetting = NotableMissSetting.TryFromValue(result.NotableMissSetting),
            //Core Game News Settings
            EnableGameNews = result.EnableGameNews,
            ShowMightReleaseInYearNews = result.ShowMightReleaseInYearNews,
            ShowWillReleaseInYearNews = result.ShowWillReleaseInYearNews,
            ShowScoreGameNews = result.ShowScoreGameNews,
            ShowAlreadyReleasedGameNews = result.ShowReleasedGameNews,
            ShowNewGameNews = result.ShowNewGameNews,
            ShowEditedGameNews = result.ShowEditedGameNews,
            SkippedTags = string.IsNullOrEmpty(result.SkippedTags)
                ? new List<MasterGameTag>()
                : ((string)result.SkippedTags).Split(',').Select(tag => tagsDictionary[tag]).ToList()
        };
    }

    public async Task<MinimalLeagueChannelRecord?> GetMinimalLeagueChannel(ulong guildID, ulong channelID)
    {
        var leagueChannelEntity = await GetLeagueChannelEntity(guildID, channelID);
        return leagueChannelEntity?.ToMinimalDomain();
    }

    private async Task<LeagueChannelEntity?> GetLeagueChannelEntity(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            guildID,
            channelID
        };

        // Update the SQL query to cast BidAlertRoleID as UNSIGNED to ensure it is returned as ulong.
        const string leagueChannelSQL = "SELECT GuildID, ChannelID, LeagueID, CAST(BidAlertRoleID AS UNSIGNED) AS BidAlertRoleID FROM tbl_discord_leaguechannel WHERE GuildID = @guildID AND ChannelID = @channelID";

        var minimalLeagueChannelRecord = await connection.QuerySingleOrDefaultAsync<MinimalLeagueChannelRecord>(leagueChannelSQL, queryObject);
        if (minimalLeagueChannelRecord == null)
        {
            _logger.Warning("No league channel found for GuildID: {GuildID}, ChannelID: {ChannelID}", guildID, channelID);
            return null;
        }

        var activeYears = await _fantasyCriticRepo.GetActiveLeagueYears(new List<Guid> { minimalLeagueChannelRecord.LeagueID });
        var currentYear = activeYears.FirstOrDefault(x => x.Year == _clock.GetToday().Year);

        if (currentYear == null)
        {
            _logger.Warning("No current year found for LeagueID: {LeagueID}", minimalLeagueChannelRecord.LeagueID);
            return null;
        }

        // Query the Game News Settings for the league channel
        var gameNewsSettings = await GetGameNewsSettings(guildID, channelID);

        // Query the League Game News Settings for the league channel
        var leagueGameNewsSettings = await GetLeagueGameNewsSettings(guildID, channelID);


        return new LeagueChannelEntity(minimalLeagueChannelRecord, activeYears.ToList(), currentYear, gameNewsSettings, leagueGameNewsSettings);
    }

    private async Task<ConferenceChannelEntity?> GetConferenceChannelEntity(ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            guildID,
            channelID
        };

        const string leagueChannelSQL = "select * from tbl_discord_conferencechannel WHERE GuildID = @guildID AND ChannelID = @channelID";

        var conferenceChannelEntity = await connection.QuerySingleOrDefaultAsync<ConferenceChannelEntity>(leagueChannelSQL, queryObject);
        return conferenceChannelEntity;
    }

    public async Task RemoveAllLeagueChannelsForLeague(Guid leagueID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            leagueID
        };
        var sql = "DELETE FROM tbl_discord_leaguechannel WHERE LeagueID=@leagueID";
        await connection.ExecuteAsync(sql, queryObject);
    }
}
