using FantasyCritic.Lib.Discord.Models;

namespace FantasyCritic.MySQL.Entities.Discord;
internal class GameNewsChannelEntity
{
    public GameNewsChannelEntity()
    {

    }

    public GameNewsChannelEntity(ulong guildID, ulong channelID, bool showWillReleaseInYearNews, bool showMightReleaseInYearNews,
        bool showWillNotReleaseInYearNews, bool showScoreGameNews, bool showReleasedGameNews, bool showNewGameNews, bool showEditedGameNews)
    {
        GuildID = guildID;
        ChannelID = channelID;
        ShowWillReleaseInYearNews = showWillReleaseInYearNews;
        ShowMightReleaseInYearNews = showMightReleaseInYearNews;
        ShowWillNotReleaseInYearNews = showWillNotReleaseInYearNews;
        ShowScoreGameNews = showScoreGameNews;
        ShowReleasedGameNews = showReleasedGameNews;
        ShowNewGameNews = showNewGameNews;
        ShowEditedGameNews = showEditedGameNews;
    }

    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }

    public bool ShowWillReleaseInYearNews { get; set; }
    public bool ShowMightReleaseInYearNews { get; set; }
    public bool ShowWillNotReleaseInYearNews { get; set; }
    public bool ShowScoreGameNews { get; set; }
    public bool ShowReleasedGameNews { get; set; }
    public bool ShowNewGameNews { get; set; }
    public bool ShowEditedGameNews { get; set; }

    public GameNewsOnlyChannelRecord ToDomain(IEnumerable<MasterGameTag> skippedTags)
    {
        var gameNewsSettings = new GameNewsSettingsRecord()
        {
            ShowWillReleaseInYearNews = ShowWillReleaseInYearNews,
            ShowMightReleaseInYearNews = ShowMightReleaseInYearNews,
            ShowWillNotReleaseInYearNews = ShowWillNotReleaseInYearNews,
            ShowScoreGameNews = ShowScoreGameNews,
            ShowReleasedGameNews = ShowReleasedGameNews,
            ShowNewGameNews = ShowNewGameNews,
            ShowEditedGameNews = ShowEditedGameNews,
        };
        return new GameNewsOnlyChannelRecord(GuildID, ChannelID, skippedTags.ToList(), gameNewsSettings);
    }
}
