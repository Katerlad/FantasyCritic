using FantasyCritic.Lib.Discord;
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.Lib.Domain;

public record MinimalLeagueChannelRecord(Guid LeagueID, ulong GuildID, ulong ChannelID, bool SendLeagueMasterGameUpdates, NotableMissesSetting NotableMissesSetting, ulong? BidAlertRoleID) : IDiscordChannel
{
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);

    public MultiYearLeagueChannel ToMultiYearLeagueChannel(IReadOnlyList<LeagueYear> activeLeagueYears)
        => new MultiYearLeagueChannel(LeagueID, activeLeagueYears, GuildID, ChannelID, SendLeagueMasterGameUpdates, NotableMissesSetting, BidAlertRoleID);
}

public record LeagueChannelRecord(LeagueYear LeagueYear, ulong GuildID, ulong ChannelID, bool SendLeagueMasterGameUpdates, NotableMissesSetting NotableMissesSetting, AdvancedGameNewsSettings gameNewsSettings, ulong? BidAlertRoleID);

public record MultiYearLeagueChannel(Guid LeagueID, IReadOnlyList<LeagueYear> ActiveLeagueYears, ulong GuildID, ulong ChannelID, bool SendLeagueMasterGameUpdates, NotableMissesSetting NotableMissesSetting, ulong? BidAlertRoleID);

public record GameNewsOnlyChannelRecord(ulong GuildID, ulong ChannelID, IReadOnlyList<MasterGameTag> SkippedTags, AdvancedGameNewsSettings AdvancedGameNewsSettings)
{
    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);
}



public class CombinedChannel
{
    public CombinedChannel(MultiYearLeagueChannel? leagueChannel, GameNewsOnlyChannelRecord? gameNewsChannel)
    {
        if (leagueChannel is null && gameNewsChannel is null)
        {
            throw new Exception("Both channel options cannot be null");
        }

        GameNewsSetting = new AdvancedGameNewsSettings(leagueChannel, gameNewsChannel);

        if (leagueChannel is not null)
        {
            GuildID = leagueChannel.GuildID;
            ChannelID = leagueChannel.ChannelID;
            LeagueID = leagueChannel.LeagueID;
            GameNewsSetting.LeagueGameNewsEnabled = leagueChannel.SendLeagueMasterGameUpdates;
            GameNewsSetting.NotableMissSetting = leagueChannel.NotableMissesSetting;
            ActiveLeagueYears = leagueChannel.ActiveLeagueYears;
        }

        if (gameNewsChannel is not null)
        {
            GuildID = gameNewsChannel.GuildID;
            ChannelID = gameNewsChannel.ChannelID;
            GameNewsSetting = gameNewsChannel.AdvancedGameNewsSettings;
            SkippedTags = gameNewsChannel.SkippedTags;
        }
        else
        {
            SkippedTags = new List<MasterGameTag>();
        }
    }

    public ulong GuildID { get; }
    public ulong ChannelID { get; }
    public Guid? LeagueID { get; }
    public IReadOnlyList<LeagueYear>? ActiveLeagueYears { get; }
    public AdvancedGameNewsSettings GameNewsSetting { get; }
    public IReadOnlyList<MasterGameTag> SkippedTags { get; }

    public DiscordChannelKey ChannelKey => new DiscordChannelKey(GuildID, ChannelID);

    public CombinedChannelGameSetting CombinedSetting => new CombinedChannelGameSetting(GameNewsSetting, SkippedTags);
}
