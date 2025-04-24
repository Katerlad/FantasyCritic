using FantasyCritic.Lib.DependencyInjection;
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Domain.Conferences;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.MySQL.Entities.Discord;

namespace FantasyCritic.MySQL;
public class MySQLDiscordRepo : IDiscordRepo
{
    private readonly IFantasyCriticRepo _fantasyCriticRepo;
    private readonly IMasterGameRepo _masterGameRepo;
    private readonly IConferenceRepo _conferenceRepo;
    private readonly ICombinedDataRepo _combinedDataRepo;
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
        _connectionString = configuration.ConnectionString;
    }

    public async Task SetLeagueChannel(Guid leagueID, ulong guildID, ulong channelID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var minimalLeagueChannelRecord = new MinimalLeagueChannelRecord(leagueID, guildID, channelID,true, NotableMissSetting.ScoreUpdates, null);
        var existingChannel = await GetLeagueChannelEntity(guildID, channelID);
        var existingChannelMinimal = existingChannel?.ToMinimalDomain();
        var sql = existingChannel == null
            ? "INSERT INTO tbl_discord_leaguechannel (GuildID,ChannelID,LeagueID,SendLeagueMasterGameUpdates,SendNotableMisses) VALUES (@GuildID, @ChannelID, @LeagueID, @SendLeagueMasterGameUpdates, @SendNotableMisses)"
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

    public async Task SetLeagueGameNewsSetting(Guid leagueID, ulong guildID, ulong channelID, bool sendLeagueMasterGameUpdates, NotableMissSetting notableMissesSetting)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var minimalLeagueChannelRecord = new MinimalLeagueChannelRecord(leagueID, guildID, channelID, sendLeagueMasterGameUpdates, notableMissesSetting, null);
        var sql = "UPDATE tbl_discord_leaguechannel SET SendLeagueMasterGameUpdates=@SendLeagueMasterGameUpdates, SendNotableMisses=@SendNotableMisses WHERE LeagueID=@LeagueID AND GuildID=@GuildID AND ChannelID=@ChannelID";
        await connection.ExecuteAsync(sql, minimalLeagueChannelRecord);
    }

    public async Task SetGameNewsSetting(ulong guildID, ulong channelID, GameNewsSettings gameNewsSettings)
    {
        bool deleting = !gameNewsSettings.AllGameUpdatesEnabled;

        var deleteChannelSQL = "DELETE FROM tbl_discord_gamenewschannel WHERE GuildID=@GuildID AND ChannelID=@ChannelID;";
        var deleteOptionsSQL = "DELETE FROM tbl_discord_gamenewsoptions WHERE GuildID=@GuildID AND ChannelID=@ChannelID;";
        var insertChannelSQL = "INSERT INTO tbl_discord_gamenewschannel (GuildID, ChannelID) VALUES (@GuildID, @ChannelID);";

        var insertOptionsSQL = @"
        REPLACE INTO tbl_discord_gamenewsoptions (
            GuildID, ChannelID,
            ShowMightReleaseInYearNews,
            ShowWillReleaseInYearNews,
            ShowScoreGameNews,
            ShowReleasedGameNews,
            ShowNewGameNews,
            ShowEditedGameNews
        ) VALUES (
            @GuildID, @ChannelID,
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

        if (!deleting)
        {
            // Insert channel row
            await connection.ExecuteAsync(insertChannelSQL, param, transaction);

            // Insert or update options row
            var optionsParam = new
            {
                GuildID = guildID,
                ChannelID = channelID,
                gameNewsSettings.ShowMightReleaseInYearNews,
                gameNewsSettings.ShowWillReleaseInYearNews,
                gameNewsSettings.ShowScoreGameNews,
                gameNewsSettings.ShowReleasedGameNews,
                gameNewsSettings.ShowNewGameNews,
                gameNewsSettings.ShowEditedGameNews
            };

            await connection.ExecuteAsync(insertOptionsSQL, optionsParam, transaction);

            // Re-insert tag skips if they existed
            await connection.BulkInsertAsync(masterGameTagEntities, "tbl_discord_gamenewschannelskiptag", 500, transaction);
        }

        await transaction.CommitAsync();
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
        var minimalLeagueChannelRecord = new MinimalLeagueChannelRecord(leagueID, guildID, channelID, true, NotableMissSetting.ScoreUpdates, bidAlertRoleID);
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

    public async Task<IReadOnlyList<MinimalLeagueChannelRecord>> GetAllMinimalLeagueChannels()
    {
        await using var connection = new MySqlConnection(_connectionString);
        const string sql = "select * from tbl_discord_leaguechannel";

        var leagueChannels = await connection.QueryAsync<LeagueChannelEntity>(sql);
        return leagueChannels.Select(l => l.ToMinimalDomain()).ToList();
    }

    public async Task<IReadOnlyList<GameNewsOnlyChannelRecord>> GetAllGameNewsChannels()
    {
        var possibleTags = await _masterGameRepo.GetMasterGameTags();

        await using var connection = new MySqlConnection(_connectionString);
        const string channelSQL = "select * from tbl_discord_gamenewschannel";
        const string tagSQL = "select * from tbl_discord_gamenewschannelskiptag";

        var channelEntities = await connection.QueryAsync<GameNewsChannelEntity>(channelSQL);
        var tagEntities = await connection.QueryAsync<GameNewsChannelSkippedTagEntity>(tagSQL);

        var tagLookup = tagEntities.ToLookup(x => new DiscordChannelKey(x.GuildID, x.ChannelID));
        List<GameNewsOnlyChannelRecord> gameNewsChannels = new List<GameNewsOnlyChannelRecord>();
        foreach (var channelEntity in channelEntities)
        {
            var tagAssociations = tagLookup[new DiscordChannelKey(channelEntity.GuildID, channelEntity.ChannelID)].Select(x => x.TagName).ToList();
            IReadOnlyList<MasterGameTag> tags = possibleTags
                .Where(x => tagAssociations.Contains(x.Name))
                .ToList();
            gameNewsChannels.Add(channelEntity.ToDomain(tags));
        }

        return gameNewsChannels;
    }

    public async Task<IReadOnlyList<MinimalLeagueChannelRecord>> GetLeagueChannels(Guid leagueID)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var queryObject = new
        {
            leagueID
        };

        const string leagueChannelSQL = "select * from tbl_discord_leaguechannel WHERE LeagueID = @leagueID";

        var leagueChannels = await connection.QueryAsync<LeagueChannelEntity>(leagueChannelSQL, queryObject);
        return leagueChannels.Select(l => l.ToMinimalDomain()).ToList();
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
        var gameNewsSettings = new GameNewsSettings
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

        const string leagueChannelSQL = "select * from tbl_discord_leaguechannel WHERE GuildID = @guildID AND ChannelID = @channelID";

        var leagueChannelEntity = await connection.QuerySingleOrDefaultAsync<LeagueChannelEntity>(leagueChannelSQL, queryObject);
        return leagueChannelEntity;
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
