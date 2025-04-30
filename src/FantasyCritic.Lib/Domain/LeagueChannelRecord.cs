using FantasyCritic.Lib.Discord;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Domain;

public record MinimalLeagueChannelRecord(
    ulong GuildID,
    ulong ChannelID,
    Guid LeagueID,
    ulong? BidAlertRoleID) : IDiscordChannel
{
    public ulong GuildID { get; set; } = GuildID;
    public ulong ChannelID { get; set; } = ChannelID;
    public Guid LeagueID { get; set; } = LeagueID;
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public ulong? BidAlertRoleID { get; set; } = BidAlertRoleID;

}

public record LeagueChannelRecord(
    ulong GuildID,
    ulong ChannelID,
    Guid LeagueID,
    LeagueYear CurrentYear,
    IReadOnlyList<LeagueYear> ActiveLeagueYears,
    LeagueGameNewsSettings LeagueGameNewsSettings,
    GameNewsSettings GameNewsSettings,
    ulong? BidAlertRoleID
)
{
    public IReadOnlyList<LeagueYear> ActiveLeagueYears { get; init; } = ActiveLeagueYears;
    public LeagueYear CurrentYear { get; init; } = CurrentYear;
    public ulong GuildID { get; init; } = GuildID;
    public ulong ChannelID { get; init; } = ChannelID;
    public Guid LeagueID { get; init; } = LeagueID;
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public LeagueGameNewsSettings LeagueGameNewsSettings { get; init; } = LeagueGameNewsSettings;
    public GameNewsSettings GameNewsSettings { get; init; } = GameNewsSettings;
    public ulong? BidAlertRoleID { get; init; } = BidAlertRoleID;
}

public record GameNewsOnlyChannelRecord(
    ulong GuildID,
    ulong ChannelID,
    IReadOnlyList<MasterGameTag> SkippedTags,
    GameNewsSettings GameNewsSettings)
{
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
}
