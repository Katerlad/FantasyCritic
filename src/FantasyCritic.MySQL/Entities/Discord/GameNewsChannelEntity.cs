namespace FantasyCritic.MySQL.Entities.Discord;
internal class GameNewsChannelEntity
{
    public GameNewsChannelEntity()
    {

    }

    public GameNewsChannelEntity(ulong guildID, ulong channelID, GameNewsSettings gameNewsSetting)
    {
        GuildID = guildID;
        ChannelID = channelID;
        GameNewsSettings = gameNewsSetting;
    }

    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }
    public GameNewsSettings GameNewsSettings { get; set; } = null!;

    public GameNewsOnlyChannelRecord ToDomain(IEnumerable<MasterGameTag> skippedTags)
    {
        return new GameNewsOnlyChannelRecord(GuildID, ChannelID, skippedTags.ToList(),GameNewsSettings);
    }
}
