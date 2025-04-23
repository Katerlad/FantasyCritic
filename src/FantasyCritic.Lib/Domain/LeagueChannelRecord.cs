using FantasyCritic.Lib.Discord;
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Domain;

public record MinimalLeagueChannelRecord(Guid LeagueID, ulong GuildID, ulong ChannelID, bool SendLeagueMasterGameUpdates, NotableMissSetting NotableMissesSetting, ulong? BidAlertRoleID) : IDiscordChannel
{
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);

    public MultiYearLeagueChannel ToMultiYearLeagueChannel(IReadOnlyList<LeagueYear> activeLeagueYears)
        => new MultiYearLeagueChannel(LeagueID, activeLeagueYears, GuildID, ChannelID, SendLeagueMasterGameUpdates, NotableMissesSetting, BidAlertRoleID);
}

public record LeagueChannelRecord(IReadOnlyList<LeagueYear> ActiveLeagueYears, LeagueYear CurrentLeagueYear, ulong GuildID, ulong ChannelID, Guid LeagueID, bool SendLeagueMasterGameUpdates, bool SendEligibleSlotGameNewsOnly, NotableMissSetting NotableMissesSetting, GameNewsSettings gameNewsSettings, ulong? BidAlertRoleID);

public record MultiYearLeagueChannel(Guid LeagueID, IReadOnlyList<LeagueYear> ActiveLeagueYears, ulong GuildID, ulong ChannelID, bool SendLeagueMasterGameUpdates, NotableMissSetting NotableMissesSetting, ulong? BidAlertRoleID);

public record GameNewsOnlyChannelRecord(ulong GuildID, ulong ChannelID, IReadOnlyList<MasterGameTag> SkippedTags, GameNewsSettings GameNewsSettings)
{
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
}
