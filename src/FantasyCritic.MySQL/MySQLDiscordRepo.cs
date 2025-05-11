using FantasyCritic.Lib.DependencyInjection;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Domain.Conferences;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.MySQL.Entities.Discord;
using Serilog;

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
        var leagueChannelEntity = new LeagueChannelEntity(guildID, channelID, leagueID, true, true, Lib.Discord.Enums.NotableMissSetting.ScoreUpdates.Value, null);
        var existingChannel = await GetLeagueChannelEntity(guildID, channelID);
        var sql = existingChannel == null
            ? "INSERT INTO tbl_discord_leaguechannel (GuildID,ChannelID,LeagueID,ShowPickedGameNews,ShowEligibleGameNews,NotableMissSetting) VALUES (@GuildID, @ChannelID, @LeagueID, @ShowPickedGameNews, @ShowEligibleGameNews, @NotableMissSetting)"
            : "UPDATE tbl_discord_leaguechannel SET LeagueID=@LeagueID WHERE ChannelID=@ChannelID AND GuildID=@GuildID";
        await connection.ExecuteAsync(sql, leagueChannelEntity);
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
        var leagueChannelEntity = new LeagueChannelEntity(guildID, channelID, leagueID, leagueGameNewsSettings.ShowPickedGameNews, leagueGameNewsSettings.ShowEligibleGameNews, leagueGameNewsSettings.NotableMissSetting.Value, null);
        var sql = """
                  UPDATE tbl_discord_leaguechannel SET 
                  ShowPickedGameNews=@ShowPickedGameNews, 
                  ShowEligibleGameNews=@ShowEligibleGameNews,
                  SendNotableMisses=@SendNotableMisses
                  WHERE LeagueID=@LeagueID AND GuildID=@GuildID AND ChannelID=@ChannelID";
                  """;
        await connection.ExecuteAsync(sql, leagueChannelEntity);
    }

    public async Task SetGameNewsSetting(ulong guildID, ulong channelID, GameNewsSettingsRecord gameNewsSettings)
    {
        var insertOrUpdateSQL = """
                                INSERT INTO tbl_discord_gamenewschannel (
                                    GuildID,
                                    ChannelID,
                                    ShowWillReleaseInYearNews,
                                    ShowMightReleaseInYearNews,
                                    ShowWillNotReleaseInYearNews,
                                    ShowScoreGameNews,
                                    ShowReleasedGameNews,
                                    ShowNewGameNews,
                                    ShowEditedGameNews
                                )
                                VALUES (
                                    @GuildID,
                                    @ChannelID,
                                    @ShowWillReleaseInYearNews,
                                    @ShowMightReleaseInYearNews,
                                    @ShowWillNotReleaseInYearNews,
                                    @ShowScoreGameNews,
                                    @ShowReleasedGameNews,
                                    @ShowNewGameNews,
                                    @ShowEditedGameNews
                                )
                                ON DUPLICATE KEY UPDATE
                                    ShowWillReleaseInYearNews = @ShowWillReleaseInYearNews,
                                    ShowMightReleaseInYearNews = @ShowMightReleaseInYearNews,
                                    ShowWillNotReleaseInYearNews = @ShowWillNotReleaseInYearNews,
                                    ShowScoreGameNews = @ShowScoreGameNews,
                                    ShowReleasedGameNews = @ShowReleasedGameNews,
                                    ShowNewGameNews = @ShowNewGameNews,
                                    ShowEditedGameNews = @ShowEditedGameNews;
                                
                                """;
        var selectTagsSQL = "SELECT * from tbl_discord_gamenewschannelskiptag where GuildID=@guildID AND ChannelID=@channelID;";
        var deleteTagsSQL = "DELETE from tbl_discord_gamenewschannelskiptag where GuildID=@guildID AND ChannelID=@channelID;";
        var gameNewsChannelEntity = new GameNewsChannelEntity(guildID, channelID, gameNewsSettings.ShowWillReleaseInYearNews, gameNewsSettings.ShowMightReleaseInYearNews,
            gameNewsSettings.ShowWillNotReleaseInYearNews, gameNewsSettings.ShowScoreGameNews, gameNewsSettings.ShowReleasedGameNews, gameNewsSettings.ShowNewGameNews, gameNewsSettings.ShowEditedGameNews);

        var param = new
        {
            guildID,
            channelID
        };

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var masterGameTagEntities = await connection.QueryAsync<GameNewsChannelSkippedTagEntity>(selectTagsSQL, param);

        await using var transaction = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(deleteTagsSQL, param, transaction);
        await connection.ExecuteAsync(insertOrUpdateSQL, gameNewsChannelEntity, transaction);
        await connection.BulkInsertAsync(masterGameTagEntities, "tbl_discord_gamenewschannelskiptag", 500, transaction);
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
        var leagueChannelEntity = new LeagueChannelEntity(guildID, channelID, leagueID, true, true, "UNUSED", bidAlertRoleID);
        var sql = "UPDATE tbl_discord_leaguechannel SET BidAlertRoleID=@BidAlertRoleID WHERE LeagueID=@LeagueID AND GuildID=@GuildID AND ChannelID=@ChannelID";
        await connection.ExecuteAsync(sql, leagueChannelEntity);
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

    public async Task<IReadOnlyList<LeagueChannelRecord>> GetAllLeagueChannels()
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<MinimalLeagueChannelRecord>> GetAllMinimalLeagueChannels()
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<GameNewsOnlyChannelRecord>> GetAllGameNewsChannels()
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<LeagueChannelRecord>> GetLeagueChannels(Guid leagueID)
    {
        throw new NotImplementedException();
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

    public async Task<MinimalLeagueChannelRecord?> GetMinimalLeagueChannel(ulong guildID, ulong channelID)
    {
        throw new NotImplementedException();
    }

    public async Task<LeagueChannelRecord?> GetLeagueChannel(ulong guildID, ulong channelID, IReadOnlyList<SupportedYear> supportedYears, int? year = null)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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

    public async Task<CompleteGameNewsSettings?> GetCompleteGameNewsSettings(ulong guildID, ulong channelID)
    {
        throw new NotImplementedException();
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
}
