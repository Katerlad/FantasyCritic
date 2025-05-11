using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Interfaces;
using System.Collections.Concurrent;

namespace FantasyCritic.Lib.Discord.Commands
{
    public class GameNewsSettingsCommand : InteractionModuleBase<SocketInteractionContext>
    {
        //Service Dependencies
        private readonly IDiscordRepo _discordRepo;

        private readonly IMasterGameRepo _masterGameRepo;

        //State
        private static IReadOnlyList<MasterGameTag>? _masterGameTags;

        /// <summary>
        /// First ulong - ChannelID, Second ulong - SnapshotMessageID
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, ulong> _channelSnapshotLookup = new();

        public GameNewsSettingsCommand(IDiscordRepo discordRepo, IMasterGameRepo masterGameRepo)
        {
            _discordRepo = discordRepo;
            _masterGameRepo = masterGameRepo;
        }

        [SlashCommand("game-news-settings", "View and Change Game News Settings for this channel.")]
        public async Task GameNewsSettings()
        {
            try
            {
                //If this is the first time interaction was called
                if (!Context.Interaction.HasResponded)
                {
                    // Defer the interaction to extend the response window
                    await DeferAsync();
                }

                //Update Master Game Tags Dictionary
                if (_masterGameTags == null)
                {
                    _masterGameTags = await _masterGameRepo.GetMasterGameTags();
                }

                // Initialize settings for this interaction
                var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(Context.Guild.Id, Context.Channel.Id);
                var commandSettings = await _discordRepo.GetCompleteGameNewsSettings(Context.Guild.Id, Context.Channel.Id);

                bool isLeagueChannel = leagueChannel != null;

                

                if (commandSettings == null)
                {
                    await SendDisabledGameNewsMessage();
                    return;
                }

                if (isLeagueChannel)
                {
                    await SendLeagueGameNewsSnapshot(commandSettings);
                }
                else
                {
                    await SendGameNewsOnlySnapShot(commandSettings); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GameNewsSettingCommand: {ex.Message}");
                await FollowupAsync("An error occurred while processing your request.", ephemeral: true);
                throw;
            }
        }

        #region SnapShot Messages

        private async Task SendGameNewsSnapShotMessage(CompleteGameNewsSettings settings)
        {
            var guildId = Context.Guild.Id;
            var channelId = Context.Channel.Id;

            var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(guildId, channelId);

            if (leagueChannel != null)
            {
                await SendLeagueGameNewsSnapshot(settings);
            }
            else
            {
                await SendGameNewsOnlySnapShot(settings);
            }
        }

        private async Task SendLeagueGameNewsSnapshot(CompleteGameNewsSettings settings)
        {
            var message = await FollowupAsync(settings.ToDiscordMessage(), components: GetLeagueSnapshotComponent());
            _channelSnapshotLookup[Context.Channel.Id] = message.Id;
        }

        private async Task SendGameNewsOnlySnapShot(CompleteGameNewsSettings settings)
        {
            var message = await FollowupAsync(settings.ToDiscordMessage(), components: GetGameNewsOnlySnapshotComponent());
            _channelSnapshotLookup[Context.Channel.Id] = message.Id;
        }

        private async Task UpdateSnapShotMessage(CompleteGameNewsSettings settings)
        {
            _channelSnapshotLookup.TryGetValue(Context.Channel.Id, out ulong snapshotMessageID);
            if (snapshotMessageID == default)
            {
                Serilog.Log.Error("Could not find the gamenews snapshot message for given channel {ChannelId}", Context.Channel.Id);
                return;
            }

            var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(Context.Guild.Id, Context.Channel.Id);

            var updatedComponents = leagueChannel == null ? GetGameNewsOnlySnapshotComponent() : GetLeagueSnapshotComponent();

            await Context.Channel.ModifyMessageAsync(snapshotMessageID, msg =>
            {
                msg.Content = settings.ToDiscordMessage();
                msg.Components = updatedComponents;
            });
        }

        #endregion SnapShot Messages

        #region SnapShot Components

        private MessageComponent GetLeagueSnapshotComponent()
        {
            return new ComponentBuilder()
                .AddRow(new ActionRowBuilder()
                    .WithButton(GetDisableGameNewsButton())
                    .WithButton(GetSetRecommendedSettingsButton()))
                .AddRow(new ActionRowBuilder().WithButton(GetChangeLeagueNewsSettingsButton()))
                .AddRow(new ActionRowBuilder().WithButton(GetChangeGameReleaseSettingsButton()))
                .AddRow(new ActionRowBuilder().WithButton(GetChangeGameUpdateSettingsButton()))
                .AddRow(new ActionRowBuilder().WithButton(GetChangeSkippedTagsSettingsButton()))
                .Build();
        }

        private MessageComponent GetGameNewsOnlySnapshotComponent()
        {
            return new ComponentBuilder()
                .AddRow(new ActionRowBuilder()
                    .WithButton(GetDisableGameNewsButton())
                    .WithButton(GetSetRecommendedSettingsButton()))
                .AddRow(new ActionRowBuilder().WithButton(GetChangeGameReleaseSettingsButton()))
                .AddRow(new ActionRowBuilder().WithButton(GetChangeGameUpdateSettingsButton()))
                .AddRow(new ActionRowBuilder().WithButton(GetChangeSkippedTagsSettingsButton()))
                .Build();
        }

        #endregion SnapShot Components

        #region Setting Category Messages

        private async Task SendDisabledGameNewsMessage()
        {
            var enableGameNewsMessage = new ComponentBuilder()
                .WithButton(GetEnableGameNewsButton())
                .Build();

            await FollowupAsync("Game News is currently off for this channel, Do you want to turn it on?:", components: enableGameNewsMessage, ephemeral: true);
        }

        private async Task SendGameNewsReleaseSettingsMessage(CompleteGameNewsSettings settings)
        {
            var gameReleaseSettingsMessage = new ComponentBuilder()
                .AddRow(new ActionRowBuilder().WithButton(GetNewGameNewsButton(settings.ShowNewGameNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetMightReleaseInYearButton(settings.ShowMightReleaseInYearNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetWillReleaseInYearButton(settings.ShowWillReleaseInYearNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetReleasedGameNewsButton(settings.ShowAlreadyReleasedGameNews)))
                .Build();

            await FollowupAsync("**Set Game News Release Settings** \n", components: gameReleaseSettingsMessage, ephemeral: true);
        }

        private async Task SendGameNewsUpdateSettingsMessage(CompleteGameNewsSettings settings)
        {
            var gameNewsUpdateSettingsMessage = new ComponentBuilder()
                .AddRow(new ActionRowBuilder().WithButton(GetScoreGameNewsButton(settings.ShowScoreGameNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetEditedGameNewsButton(settings.ShowEditedGameNews)))
                .Build();

            await FollowupAsync("**Set Game News Update Settings** \n", components: gameNewsUpdateSettingsMessage, ephemeral: true);
        }

        private async Task SendLeagueGameNewsSettingsMessage(CompleteGameNewsSettings settings)
        {
            var leagueGameNewsSettingsMessage = new ComponentBuilder()
                .AddRow(new ActionRowBuilder().WithButton(GetEnablePickedGameNewsButton(settings.ShowPickedGameNews ?? false)))
                .AddRow(new ActionRowBuilder().WithButton(GetEnableEligibleLeagueGameNewsOnlyButton(settings.ShowEligibleGameNews ?? false)))
                .AddRow(new ActionRowBuilder().WithButton(GetCurrentYearGameNewsOnlyButton(settings.ShowCurrentYearGameNewsOnly ?? false)))
                .AddRow(new ActionRowBuilder().WithSelectMenu(GetNotableMissSettingSelection(settings.NotableMissSetting ?? NotableMissSetting.None)))
                .Build();
            await FollowupAsync("**Set League Game News Settings** \n", components: leagueGameNewsSettingsMessage, ephemeral: true);
        }

        private async Task SendGameNewsSkipTagsSettingsMessage(CompleteGameNewsSettings settings)
        {
            var gameNewsSkipTagsSettingsMessage = new ComponentBuilder()
                .AddRow(new ActionRowBuilder().WithSelectMenu(GetSkippedTagsSelection(settings.SkippedTags)))
                .Build();
            await FollowupAsync("**Set Game News Skip Tags Settings** \n", components: gameNewsSkipTagsSettingsMessage, ephemeral: true);
        }

        #endregion Setting Category Messages

        #region Handle Interactions

        [ComponentInteraction("button_*")]
        public async Task HandleButtonPresses(string button)
        {
            // Defer the interaction response to extend the response window
            await DeferAsync();

            var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(Context.Guild.Id, Context.Channel.Id);

            //Check for existing settings in the database
            var settings = await _discordRepo.GetCompleteGameNewsSettings(Context.Guild.Id, Context.Channel.Id);

            if(_masterGameTags == null)
            {
               _masterGameTags = await _masterGameRepo.GetMasterGameTags();
            }

            //Special condition for enable game news, as this may be the first time the channel had news set up
            // so we will make a new settings class, and set it default.
            if (button == "enable_game_news")
            {
                if (settings == null)
                {
                    settings = new CompleteGameNewsSettings();

                    if (leagueChannel != null)
                    {
                        settings.SetLeagueRecommendedSettings();
                    }
                    else
                    {
                        settings.Recommended = true;
                    }
                }

                await CreateNewGameNewsChannel(settings);
                await SendGameNewsSnapShotMessage(settings);
                return;
            }

           
            //If settings is null here, all buttons below rely on the settings not being null, so error and return
            if (settings == null)
            {
                await FollowupAsync("No Game News Settings were found in the database, If this persists contact Fantasy Critic Support",ephemeral: true);
                return;
            }

            //Get snapshot message ID for any buttons that will update snapshot
            _channelSnapshotLookup.TryGetValue(Context.Channel.Id, out var snapshotMessageID);

            // Toggle the specified setting
            switch (button)
            {
                case "change_league_news_settings":
                    await SendLeagueGameNewsSettingsMessage(settings);
                    break;

                case "change_game_release_settings":
                    await SendGameNewsReleaseSettingsMessage(settings);
                    break;

                case "change_game_update_settings":
                    await SendGameNewsUpdateSettingsMessage(settings);
                    break;

                case "change_skipped_tags_settings":
                    await SendGameNewsSkipTagsSettingsMessage(settings);
                    break;

                case "disable_game_news":
                    try
                    {
                        await Context.Channel.DeleteMessageAsync(snapshotMessageID);
                    }
                    catch(Exception ex)
                    {
                        await FollowupAsync($"There was an error trying to delete the original snapshot message Error:{ex.Message}", ephemeral: true);
                        break;
                    }
                    settings.EnableGameNews = false;
                    await DeleteExistingGameNewsChannel();
                    await FollowupAsync("Game News has been disabled", ephemeral: true);
                    break;

                case "set_recommended_settings":
                    settings.Recommended = true;
                    await UpdateGameNewsSettings(settings);
                    await UpdateSnapShotMessage(settings);
                    await FollowupAsync("Recommended settings have been set", ephemeral: true);
                    break;

                case "picked_game_news":
                    if (settings.ShowPickedGameNews.HasValue)
                    {
                        settings.ShowPickedGameNews = !settings.ShowPickedGameNews.Value;
                        await UpdateGameNewsSettings(settings);
                        await UpdateButtonState("picked_game_news", settings.ShowPickedGameNews.Value);
                        await UpdateSnapShotMessage(settings);
                    }
                    break;

                case "eligible_game_news":
                    if (settings.ShowEligibleGameNews.HasValue)
                    {
                        settings.ShowEligibleGameNews = !settings.ShowEligibleGameNews.Value;
                        await UpdateGameNewsSettings(settings);
                        await UpdateButtonState("eligible_game_news", settings.ShowEligibleGameNews.Value);
                        await UpdateSnapShotMessage(settings);
                    }
                    break;

                case "current_year_game_news":
                    if (settings.ShowCurrentYearGameNewsOnly.HasValue)
                    {
                        settings.ShowCurrentYearGameNewsOnly = !settings.ShowCurrentYearGameNewsOnly.Value;
                        await UpdateGameNewsSettings(settings);
                        await UpdateButtonState("current_year_game_news", settings.ShowCurrentYearGameNewsOnly.Value);
                        await UpdateSnapShotMessage(settings);
                    }
                    break;

                case "might_release_in_year":
                    settings.ShowMightReleaseInYearNews = !settings.ShowMightReleaseInYearNews;
                    await UpdateGameNewsSettings(settings);
                    await UpdateButtonState("might_release_in_year", settings.ShowMightReleaseInYearNews);
                    await UpdateSnapShotMessage(settings);
                    break;

                case "will_release_in_year":
                    settings.ShowWillReleaseInYearNews = !settings.ShowWillReleaseInYearNews;
                    await UpdateGameNewsSettings(settings);
                    await UpdateButtonState("will_release_in_year", settings.ShowWillReleaseInYearNews);
                    await UpdateSnapShotMessage(settings);
                    break;

                case "score_game_news":
                    settings.ShowScoreGameNews = !settings.ShowScoreGameNews;
                    await UpdateGameNewsSettings(settings);
                    await UpdateButtonState("score_game_news", settings.ShowScoreGameNews);
                    await UpdateSnapShotMessage(settings);
                    break;

                case "released_game_news":
                    settings.ShowAlreadyReleasedGameNews = !settings.ShowAlreadyReleasedGameNews;
                    await UpdateGameNewsSettings(settings);
                    await UpdateButtonState("released_game_news", settings.ShowAlreadyReleasedGameNews);
                    await UpdateSnapShotMessage(settings);
                    break;

                case "new_game_news":
                    settings.ShowNewGameNews = !settings.ShowNewGameNews;
                    await UpdateGameNewsSettings(settings);
                    await UpdateButtonState("new_game_news", settings.ShowNewGameNews);
                    await UpdateSnapShotMessage(settings);
                    break;

                case "edited_game_news":
                    settings.ShowEditedGameNews = !settings.ShowEditedGameNews;
                    await UpdateGameNewsSettings(settings);
                    await UpdateButtonState("edited_game_news", settings.ShowEditedGameNews);
                    await UpdateSnapShotMessage(settings);
                    break;
            }
        }

        [ComponentInteraction("selection_*")]
        public async Task HandleSelectMenu(string selection)
        {
            // Defer the interaction response to extend the response window
            await DeferAsync();

            // Cast the interaction to SocketMessageComponent
            var component = Context.Interaction as SocketMessageComponent;

            if (component == null)
            {
                await FollowupAsync("Failed to process the select menu interaction.", ephemeral: true);
                return;
            }

            if (_masterGameTags == null)
            {
                _masterGameTags = await _masterGameRepo.GetMasterGameTags();
            }

            // Retrieve the selected values
            var selectedValues = component.Data.Values;

            CompleteGameNewsSettings? settings;
            try
            {
                settings = await _discordRepo.GetCompleteGameNewsSettings(Context.Guild.Id, Context.Channel.Id);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error retrieving game news settings for channel {ChannelId}", Context.Channel.Id);
                await FollowupAsync("Failed to retrieve game news settings.", ephemeral: true);
                return;
            }

            if (settings == null)
            {
                await FollowupAsync("Settings could not be found for this interaction.", ephemeral: true);
                return;
            }


            switch (selection)
            {
                case "notable_miss":
                    settings.NotableMissSetting = NotableMissSetting.TryFromValue(selectedValues.FirstOrDefault() ?? "");
                    await UpdateGameNewsSettings(settings);
                    await UpdateSnapShotMessage(settings);
                    break;

                case "skipped_tags":
                    var selectedTags = _masterGameTags?
                        .Where(tag => selectedValues.Contains(tag.Name))
                        .ToList();

                    settings.SkippedTags = selectedTags ?? settings.SkippedTags;

                    await UpdateGameNewsSettings(settings);
                    await UpdateSnapShotMessage(settings);
                    break;

                default:
                    await FollowupAsync($"Couldn't handle Selection menu: {selection} ", ephemeral: true);
                    Serilog.Log.Error($"Couldn't handle Selection Menu: {selection}");
                    return;
            }
        }

        private async Task UpdateButtonState(string buttonId, bool newState)
        {
            // Retrieve the original response message
            var originalMessage = await Context.Interaction.GetOriginalResponseAsync();

            // Create a new ComponentBuilder to rebuild the components
            var updatedComponentBuilder = new ComponentBuilder();

            // Iterate through the existing components
            foreach (var actionRow in originalMessage.Components)
            {
                var actionRowBuilder = new ActionRowBuilder();

                foreach (var component in actionRow.Components)
                {
                    if (component is ButtonComponent button && button.CustomId == "button_" + buttonId)
                    {
                        // Replace the target button with the updated button
                        var updatedButton = new ButtonBuilder()
                            .WithCustomId(button.CustomId)
                            .WithLabel(button.Label)
                            .WithEmote(new Emoji(newState ? "✅" : "❌"))
                            .WithStyle(button.Style);

                        actionRowBuilder.WithButton(updatedButton);
                    }
                    else
                    {
                        // Add the existing button or other components as-is
                        actionRowBuilder.AddComponent(component);
                    }
                }

                // Add the updated action row to the component builder
                updatedComponentBuilder.AddRow(actionRowBuilder);
            }

            // Modify the original response with the updated components
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Components = updatedComponentBuilder.Build();
            });
        }

        #endregion Handle Interactions

        #region RepoHelpers

        private async Task CreateNewGameNewsChannel(CompleteGameNewsSettings settings)
        {
            var guildID = Context.Guild.Id;
            var channelID = Context.Channel.Id;

            var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(guildID, channelID);

            if (leagueChannel != null)
            {
                await _discordRepo.SetGameNewsSetting(guildID, channelID, settings.ToGameNewsSettings());
                await _discordRepo.SetLeagueGameNewsSetting(
                    leagueChannel.LeagueID,
                    guildID,
                    channelID,
                    new LeagueGameNewsSettingsRecord(
                        settings.ShowPickedGameNews ?? false,
                        settings.ShowEligibleGameNews ?? false,
                        settings.NotableMissSetting ?? NotableMissSetting.None
                    ));

            }
            else
            {
                await _discordRepo.SetGameNewsSetting(guildID, channelID, settings.ToGameNewsSettings());
            }
        }

        private async Task DeleteExistingGameNewsChannel()
        {
            var guildID = Context.Guild.Id;
            var channelId = Context.Channel.Id;
            await _discordRepo.DeleteGameNewsChannel(guildID, channelId);
        }

        private async Task UpdateGameNewsSettings(CompleteGameNewsSettings settings)
        {
            var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(Context.Guild.Id, Context.Channel.Id);

            if (leagueChannel != null)
            {
                await _discordRepo.SetLeagueGameNewsSetting(
                    leagueChannel.LeagueID,
                    leagueChannel.GuildID,
                    leagueChannel.ChannelID,
                    new LeagueGameNewsSettingsRecord(
                        settings.ShowPickedGameNews ?? false,
                        settings.ShowEligibleGameNews ?? false,
                        settings.NotableMissSetting ?? NotableMissSetting.None
                        )
                    );
            }

            await _discordRepo.SetGameNewsSetting(Context.Guild.Id, Context.Channel.Id, settings.ToGameNewsSettings());
        }

        #endregion RepoHelpers

        #region Button Builders

        private ButtonBuilder GetChangeLeagueNewsSettingsButton()
        {
            return new ButtonBuilder()
               .WithCustomId("button_change_league_news_settings")
               .WithLabel("Change League News Settings")
               .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetChangeGameReleaseSettingsButton()
        {
            return new ButtonBuilder()
               .WithCustomId("button_change_game_release_settings")
               .WithLabel("Change Game Release Settings")
               .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetChangeGameUpdateSettingsButton()
        {
            return new ButtonBuilder()
                .WithCustomId("button_change_game_update_settings")
                .WithLabel("Change Game Update Settings")
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetChangeSkippedTagsSettingsButton()
        {
            return new ButtonBuilder()
                .WithCustomId("button_change_skipped_tags_settings")
                .WithLabel("Change Skipped Tags Settings")
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetEnableGameNewsButton()
        {
            return new ButtonBuilder()
                .WithCustomId("button_enable_game_news")
                .WithLabel("Enable Game News")
                .WithStyle(ButtonStyle.Success);
        }

        private ButtonBuilder GetDisableGameNewsButton()
        {
            return new ButtonBuilder()
                .WithCustomId("button_disable_game_news")
                .WithLabel("Disable Game News")
                .WithStyle(ButtonStyle.Danger);
        }

        private ButtonBuilder GetSetRecommendedSettingsButton()
        {
            return new ButtonBuilder()
                .WithCustomId("button_set_recommended_settings")
                .WithLabel("Set Recommended Settings")
                .WithStyle(ButtonStyle.Success);
        }

        private ButtonBuilder GetEnablePickedGameNewsButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_picked_game_news")
                .WithLabel("Enable Picked Game News")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetEnableEligibleLeagueGameNewsOnlyButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_eligible_game_news")
                .WithLabel("Enable Eligible League Game News")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetCurrentYearGameNewsOnlyButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_current_year_game_news")
                .WithLabel("Enable Current Year Game News Only")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetMightReleaseInYearButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_might_release_in_year")
                .WithLabel("Enable Might Release in Year")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetWillReleaseInYearButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_will_release_in_year")
                .WithLabel("Enable Will Release in Year")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetNewGameNewsButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_new_game_news")
                .WithLabel("Enable New Game News")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetEditedGameNewsButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_edited_game_news")
                .WithLabel("Enable Edited Game News")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetReleasedGameNewsButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_released_game_news")
                .WithLabel("Enable Released Game News")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        private ButtonBuilder GetScoreGameNewsButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_score_game_news")
                .WithLabel("Enable Score Game News")
                .WithEmote(new Emoji(initialSetting ? "✅" : "❌"))
                .WithStyle(ButtonStyle.Primary);
        }

        #endregion Button Builders

        #region Select Menu Builders

        private SelectMenuBuilder GetNotableMissSettingSelection(NotableMissSetting defaultSetting)
        {
            return new SelectMenuBuilder()
                .WithCustomId("selection_notable_miss")
                .WithPlaceholder("Notable Miss Options")
                .WithMinValues(1)
                .WithMaxValues(1)
                .AddOption("NotableMissSetting.InitialScore", "InitialScore", description: NotableMissSetting.InitialScore.Description, isDefault: defaultSetting == NotableMissSetting.InitialScore)
                .AddOption("NotableMissSetting.ScoreUpdates", "ScoreUpdates", description: NotableMissSetting.ScoreUpdates.Description, isDefault: defaultSetting == NotableMissSetting.ScoreUpdates)
                .AddOption("NotableMissSetting.None", "None", description: NotableMissSetting.None.Description, isDefault: defaultSetting == NotableMissSetting.None);
        }

        private SelectMenuBuilder GetSkippedTagsSelection(List<MasterGameTag>? skippedTags)
        {
            if (_masterGameTags == null)
            {
                Serilog.Log.Error("Master Game Tags are null when trying to build Skipped Tags Select Menu.");
                throw new Exception("Master Game Tags are null when trying to build Skipped Tags Select Menu.");
            }

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("selection_skipped_tags")
                .WithPlaceholder("Select Skipped Tags")
                .WithMinValues(0)
                .WithMaxValues(_masterGameTags.Count);

            foreach (var tag in _masterGameTags)
            {
                var truncatedDescription = tag.Description.Length > 95
                    ? tag.Description.Substring(0, 95) + "..."
                    : tag.Description;

                selectMenu.AddOption(tag.ReadableName, tag.Name, description: truncatedDescription, isDefault: skippedTags?.Contains(tag));
            }
            return selectMenu;
        }

        #endregion Select Menu Builders
    }
}
