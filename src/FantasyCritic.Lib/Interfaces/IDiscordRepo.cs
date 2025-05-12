
using FantasyCritic.Lib.Discord.Models;
namespace FantasyCritic.Lib.Interfaces;
public interface IDiscordRepo
{
    Task SetLeagueChannel(Guid leagueID, ulong guildID, ulong channelID);
    Task SetConferenceChannel(Guid conferenceID, ulong guildID, ulong channelID);
    Task SetLeagueGameNewsSetting(Guid leagueID, ulong guildID, ulong channelID,LeagueGameNewsSettingsRecord leagueGameNewsSettings);
    Task SetGameNewsSetting(ulong guildID, ulong channelID, GameNewsSettingsRecord gameNewsSettings);
    Task SetSkippedGameNewsTags(ulong guildID, ulong channelID, IEnumerable<MasterGameTag> skippedTags);
    Task SetBidAlertRoleId(Guid leagueID, ulong guildID, ulong channelID, ulong? bidAlertRoleID);
    Task<bool> DeleteLeagueChannel(ulong guildID, ulong channelID);
    Task<bool> DeleteConferenceChannel(ulong guildID, ulong channelID);
    Task<bool> DeleteGameNewsChannel(ulong guildID, ulong channelID);
    Task<IReadOnlyList<LeagueChannelRecord>> GetAllLeagueChannels();
    Task<IReadOnlyList<MinimalLeagueChannelRecord>> GetAllMinimalLeagueChannels();
    Task<IReadOnlyList<GameNewsOnlyChannelRecord>> GetAllGameNewsChannels();
    Task<IReadOnlyList<LeagueChannelRecord>> GetLeagueChannels(Guid leagueID);
    Task<IReadOnlyList<MinimalConferenceChannel>> GetConferenceChannels(Guid conferenceID);
    Task<MinimalLeagueChannelRecord?> GetMinimalLeagueChannel(ulong guildID, ulong channelID);
    Task<LeagueChannelRecord?> GetLeagueChannel(ulong guildID, ulong channelID, int? year = null);
    Task<ConferenceChannel?> GetConferenceChannel(ulong guildID, ulong channelID, IReadOnlyList<SupportedYear> supportedYears, int? year = null);
    Task<GameNewsOnlyChannelRecord?> GetGameNewsChannel(ulong guildID, ulong channelID);
    Task RemoveAllLeagueChannelsForLeague(Guid leagueID);
    Task<CompleteGameNewsSettings?> GetCompleteGameNewsSettings(ulong guildID, ulong channelID);
}
