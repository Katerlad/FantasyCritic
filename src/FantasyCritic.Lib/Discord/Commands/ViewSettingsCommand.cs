using Discord;
using Discord.Interactions;
using DiscordDotNetUtilities.Interfaces;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Discord.UrlBuilders;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.Lib.Services;
using JetBrains.Annotations;

namespace FantasyCritic.Lib.Discord.Commands;
public class ViewSettingsCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IDiscordRepo _discordRepo;
    private readonly InterLeagueService _interLeagueService;
    private readonly IDiscordFormatter _discordFormatter;
    private readonly string _baseAddress;
    private const string NoRoleSetText = "No role is set for alerting on public bids. You may use `/set-public-bid-role` to set up a role to mention with public bid announcements. Make sure that role is mentionable!";

    public ViewSettingsCommand(IDiscordRepo discordRepo,
        InterLeagueService interLeagueService,
        IDiscordFormatter discordFormatter,
        FantasyCriticSettings fantasyCriticSettings)
    {
        _discordRepo = discordRepo;
        _interLeagueService = interLeagueService;
        _discordFormatter = discordFormatter;
        _baseAddress = fantasyCriticSettings.BaseAddress;
    }

    [UsedImplicitly]
    [SlashCommand("view-settings", "View current bot settings.")]
    public async Task ViewSettings()
    {
        await DeferAsync();

        var supportedYears = await _interLeagueService.GetSupportedYears();
        var leagueChannel = await _discordRepo.GetLeagueChannel(Context.Guild.Id, Context.Channel.Id, supportedYears);
        var gameNewsChannel = await _discordRepo.GetGameNewsChannel(Context.Guild.Id, Context.Channel.Id);

        var embedFieldBuilders = new List<EmbedFieldBuilder>();

        var leagueDisplay = leagueChannel?.CurrentLeagueYear != null
            ? new LeagueUrlBuilder(_baseAddress, leagueChannel.CurrentLeagueYear.League.LeagueID,
                leagueChannel.CurrentLeagueYear.Year).BuildUrl(leagueChannel.CurrentLeagueYear.League.LeagueName)
            : "No league has been set. Use `/set-league` to configure a league.";

        var settingsMessage = "";

        embedFieldBuilders.Add(new EmbedFieldBuilder
        {
            Name = "League",
            Value = leagueDisplay,
            IsInline = false
        });

        if (leagueChannel?.CurrentLeagueYear != null)
        {
            embedFieldBuilders.Add(new EmbedFieldBuilder
            {
                Name = "Game News",
                Value = GetGameNewsSettingDescription( gameNewsChannel?.GameNewsSettings, gameNewsChannel?.SkippedTags),
                IsInline = false
            });

            var publicBiddingRoleDisplay =
                GetPublicBiddingRoleDisplayText(leagueChannel.CurrentLeagueYear.Options.PickupSystem,
                    leagueChannel.BidAlertRoleID);
            embedFieldBuilders.Add(new EmbedFieldBuilder
            {
                Name = "Public Bid Role",
                Value = publicBiddingRoleDisplay,
                IsInline = false
            });
        }

        await FollowupAsync(embed: _discordFormatter.BuildRegularEmbedWithUserFooter(
            "Fantasy Critic Bot Settings",
            settingsMessage,
            Context.User,
            embedFieldBuilders));
    }

    private static string GetGameNewsSettingDescription(GameNewsSettings? gameNewsSetting, IReadOnlyList<MasterGameTag>? skippedTags)
    {
        //TODO: Fix these messages to account for the new setting structure
        var sendLeagueMasterGameUpdates = true;
        var parts = new List<string>
        {
            sendLeagueMasterGameUpdates
                ? "✅ League Master Game Updates"
                : "❌ League Master Game Updates",
        };
        if (gameNewsSetting is null)
        {
            parts.Add("❌ Non-League Master Game Updates");
            return string.Join("\n", parts);
        }

        parts.Add(gameNewsSetting.ShowMightReleaseInYearNews
            ? "✅ Any 'Might Release' Master Game Updates"
            : "❌ Any 'Might Release' Master Game Updates");

        parts.Add(gameNewsSetting.ShowWillReleaseInYearNews
            ? "✅ Any 'Will Release' Master Game Updates"
            : "❌ Any 'Will Release' Master Game Updates");

        parts.Add(gameNewsSetting.ShowScoreGameNews
            ? "✅ Any 'Score' Master Game Updates"
            : "❌ Any 'Score' Master Game Updates");

        parts.Add(gameNewsSetting.ShowReleasedGameNews
            ? "✅ Any 'Released' Master Game Updates"
            : "❌ Any 'Released' Master Game Updates");

        parts.Add(gameNewsSetting.ShowNewGameNews
            ? "✅ Any 'New' Master Game Updates"
            : "❌ Any 'New' Master Game Updates");

        parts.Add(gameNewsSetting.ShowEditedGameNews
            ? "✅ Any 'Edited' Master Game Updates"
            : "❌ Any 'Edited' Master Game Updates");

        parts.Add(skippedTags != null
            ? $"✅ Skipping Tags: {(skippedTags.Any() ? string.Join(", ", skippedTags.Select(t => t.ReadableName)) : "NONE")}"
            : "❌ Skipping Tags");

        parts.Add("✅ for ON, ❌ for OFF");

        return string.Join("\n", parts);
    }

    private string GetPublicBiddingRoleDisplayText(PickupSystem pickupSystem, ulong? publicBiddingAlertRoleId)
    {
        if (pickupSystem.Equals(PickupSystem.SecretBidding))
        {
            return "N/A (Bidding is Secret in this league)";
        }

        if (publicBiddingAlertRoleId == null)
        {
            return
                NoRoleSetText;
        }
        var roleToMention = Context.Guild.Roles.FirstOrDefault(r => r.Id == publicBiddingAlertRoleId);
        return roleToMention != null
            ? roleToMention.Mention
            : NoRoleSetText;
    }
}
