

namespace FantasyCritic.MySQL.Entities.Discord;
internal class GameNewsOptionsEntity
{
    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }
    public bool EnableGameNews { get; set; }
    public bool ShowMightReleaseInYearNews { get; set; }
    public bool ShowWillReleaseInYearNews { get; set; }
    public bool ShowScoreGameNews { get; set; }
    public bool ShowReleasedGameNews { get; set; }
    public bool ShowNewGameNews { get; set; }
    public bool ShowEditedGameNews { get; set; }
}
