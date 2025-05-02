namespace FantasyCritic.MySQL.Entities.Discord;
internal class GameNewsChannelEntity
{

    public GameNewsChannelEntity(ulong guildID, ulong channelID)
    {
        GuildID = guildID;
        ChannelID = channelID;
    }

    public ulong GuildID { get; set; }
    public ulong ChannelID { get; set; }

    public GameNewsOnlyChannelRecord ToDomain(IEnumerable<MasterGameTag> skippedTags, GameNewsSettingsRecord? gameNewsSettings)
    {
        return new GameNewsOnlyChannelRecord(GuildID, ChannelID, skippedTags.ToList(), gameNewsSettings);
    }
}
