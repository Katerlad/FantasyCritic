using FantasyCritic.Lib.Discord;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Domain;

public record MinimalLeagueChannelRecord(ulong GuildID, ulong ChannelID, Guid LeagueID, ulong? BidAlertRoleID) : IDiscordChannel
{
    public ulong GuildID { get; set; } = GuildID;
    public ulong ChannelID { get; set; } = ChannelID;
    public Guid LeagueID { get; set; } = LeagueID;
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
    public ulong? BidAlertRoleID { get; set; } = BidAlertRoleID;

}

public record LeagueChannelRecord(IReadOnlyList<LeagueYear> ActiveLeagueYears, LeagueYear CurrentLeagueYear, ulong GuildID, ulong ChannelID, Guid LeagueID, GameNewsSettings gameNewsSettings, ulong? BidAlertRoleID);



public record GameNewsOnlyChannelRecord(ulong GuildID, ulong ChannelID, IReadOnlyList<MasterGameTag> SkippedTags, GameNewsSettings GameNewsSettings)
{
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
}
