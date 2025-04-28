using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FantasyCritic.Lib.Discord.Enums;
using FantasyCritic.Lib.Discord.Models;
using FantasyCritic.Lib.Interfaces;
using System.Collections.Concurrent;

namespace FantasyCritic.Lib.Discord.Commands
{
    public class SetGameNewsAdvancedCommand : InteractionModuleBase<SocketInteractionContext>
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

        /// <summary>
        /// First - channelId, second - settingsCache
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, GameNewsAdvancedCommandSettings> _settingsDictionary = new();

        public SetGameNewsAdvancedCommand(IDiscordRepo discordRepo, IMasterGameRepo masterGameRepo)
        {
            
            _discordRepo = discordRepo;
            _masterGameRepo = masterGameRepo;
        }

        [SlashCommand("set-game-news-advanced", "Set advanced game news settings.")]
        public async Task SetGameNewsAdvanced()
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
                var commandSettings = await _discordRepo.GetGameNewsAdvancedCommandSettings(Context.Guild.Id, Context.Channel.Id);

                bool isLeagueChannel = leagueChannel != null;

                if (commandSettings == null)
                {
                    commandSettings = new GameNewsAdvancedCommandSettings();
                    //await _discordRepo.SetLeagueGameNewsSetting();
                    await _discordRepo.SetGameNewsSetting(Context.Guild.Id, Context.Channel.Id, commandSettings.ToGameNewsSettings());
                }

                //Set Settings Dictionary
                if (!_settingsDictionary.ContainsKey(Context.Channel.Id))
                {
                    bool addedToSettingsDict = _settingsDictionary.TryAdd(Context.Channel.Id, commandSettings);

                    if (addedToSettingsDict == false)
                    {
                        await FollowupAsync("Something went wrong, please try resending command", ephemeral: true);
                    }
                }
                

                //Start Interaction Expiration timer to Clean up Dictionary after set time.
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(15));
                    if (_settingsDictionary.ContainsKey(Context.Channel.Id))
                    {
                        bool success = _settingsDictionary.TryRemove(Context.Channel.Id, out _);
                        if (!success) Serilog.Log.Error("Something went wrong removing Setting entry from dictionary after message expiration");
                    }
                });
                   

                if (isLeagueChannel)
                {
                    // If game news is currently off, show only the enable option
                    if (commandSettings.EnableGameNews == false)
                    {
                        await SendDisabledGameNewsMessage(commandSettings);
                    }
                    else
                    {
                        await SendLeagueGameNewsSnapshot(commandSettings);
                    }
                }
                else
                {
                    if (commandSettings.EnableGameNews == false)
                    {
                        await SendDisabledGameNewsMessage(commandSettings);
                    }
                    else
                    {
                        await SendGameNewsOnlySnapShot(commandSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetGameNewsAdvanced: {ex.Message}");
                await FollowupAsync("An error occurred while processing your request.", ephemeral: true);
                throw;
            }
        }

        #region SnapShot Messages
        private async Task SendGameNewsSnapShotMessage(GameNewsAdvancedCommandSettings settings)
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
        private async Task SendLeagueGameNewsSnapshot(GameNewsAdvancedCommandSettings settings)
        {
            var message = await FollowupAsync(settings.ToDiscordMessage(), components: GetLeagueSnapshotComponent());
            _channelSnapshotLookup.TryAdd(Context.Channel.Id, message.Id);
        }
        private async Task SendGameNewsOnlySnapShot(GameNewsAdvancedCommandSettings settings)
        {
           var message = await FollowupAsync(settings.ToDiscordMessage(), components: GetGameNewsOnlySnapshotComponent());
            _channelSnapshotLookup.TryAdd(Context.Channel.Id, message.Id);
        }
        private async Task UpdateSnapShotMessage(GameNewsAdvancedCommandSettings settings)
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
        #endregion

        #region Setting Category Messages 

        private async Task SendDisabledGameNewsMessage(GameNewsAdvancedCommandSettings commandSettings)
        {
            var enableGameNewsMessage = new ComponentBuilder()
                .WithButton(GetEnableGameNewsButton())
                .Build();

            await FollowupAsync("Game News is currently off for this channel, Do you want to turn it on?:", components: enableGameNewsMessage, ephemeral: true);
        }

        private async Task SendGameNewsReleaseSettingsMessage(GameNewsAdvancedCommandSettings settings)
        {
            var gameReleaseSettingsMessage = new ComponentBuilder()
                .AddRow(new ActionRowBuilder().WithButton(GetNewGameNewsButton(settings.ShowNewGameNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetMightReleaseInYearButton(settings.ShowMightReleaseInYearNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetWillReleaseInYearButton(settings.ShowWillReleaseInYearNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetReleasedGameNewsButton(settings.ShowAlreadyReleasedGameNews)))
                .Build();

            await FollowupAsync("**Set Game News Release Settings** \n", components: gameReleaseSettingsMessage, ephemeral: true);
        }

        private async Task SendGameNewsUpdateSettingsMessage(GameNewsAdvancedCommandSettings settings)
        {
            var gameNewsUpdateSettingsMessage = new ComponentBuilder()
                .AddRow(new ActionRowBuilder().WithButton(GetEditedGameNewsButton(settings.ShowEditedGameNews)))
                .AddRow(new ActionRowBuilder().WithButton(GetScoreGameNewsButton(settings.ShowScoreGameNews)))
                .Build();

            await FollowupAsync("**Set Game News Update Settings** \n", components: gameNewsUpdateSettingsMessage, ephemeral: true);
        }

        private async Task SendLeagueGameNewsSettingsMessage(GameNewsAdvancedCommandSettings settings)
        {
            var leagueGameNewsSettingsMessage = new ComponentBuilder()
                .AddRow(new ActionRowBuilder().WithButton(GetEnableEligibleLeagueGameNewsOnlyButton(settings.ShowEligibleGameNewsOnly ?? false)))
                .AddRow(new ActionRowBuilder().WithSelectMenu(GetNotableMissSettingSelection(settings.NotableMissSetting ?? NotableMissSetting.None)))
                .Build();
            await FollowupAsync("**Set League Game News Settings** \n", components: leagueGameNewsSettingsMessage, ephemeral: true);
        }

        private async Task SendGameNewsSkipTagsSettingsMessage(GameNewsAdvancedCommandSettings settings)
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

            // Retrieve the settings for this message
            if (!_settingsDictionary.TryGetValue(Context.Channel.Id, out var settings))
            {
                await FollowupAsync("Settings could not be found for this interaction, Possibly time expired since command was first called.", ephemeral: true);
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

                case "enable_game_news":
                    settings.EnableGameNews = !settings.EnableGameNews;
                    await CreateNewGameNewsChannel(settings);
                    await SendGameNewsSnapShotMessage(settings);
                    break;

                case "disable_game_news":
                    await DeleteExistingGameNewsChannel();
                    await Context.Channel.DeleteMessageAsync(snapshotMessageID);
                    await FollowupAsync("Game News has been disabled", ephemeral: true);
                    break;
                case "set_recommended_settings":
                    settings.Recommended = true;
                    await UpdateGameNewsSettings(settings);
                    await UpdateSnapShotMessage(settings);
                    await FollowupAsync("Recommended settings have been set", ephemeral: true);
                    break;
                case "eligible_game_news":
                    settings.ShowEligibleGameNewsOnly = !settings.ShowEligibleGameNewsOnly;
                    await UpdateGameNewsSettings(settings);
                    await UpdateButtonState("eligible_game_news", settings.ShowEligibleGameNewsOnly ?? false);
                    await UpdateSnapShotMessage(settings);
                    var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(Context.Guild.Id, Context.Channel.Id);
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

            // Retrieve the selected values
            var selectedValues = component.Data.Values;

            // Retrieve the settings for this message
            if (!_settingsDictionary.TryGetValue(Context.Channel.Id, out var settings))
            {
                await FollowupAsync("Settings could not be found for this interaction.", ephemeral: true);
                return;
            }

            switch (selection)
            {
                case "notable_miss":
                    settings.NotableMissSetting = NotableMissSetting.TryFromValue(selectedValues.FirstOrDefault() ?? "");
                    break;
                case "skipped_tags":
                    var selectedTags = _masterGameTags?
                        .Where(tag => selectedValues.Contains(tag.Name))
                        .ToList();

                    settings.SkippedTags = selectedTags;

                    await UpdateGameNewsSettings(settings);
                    await UpdateSnapShotMessage(settings);
                    break;

                default:
                    await FollowupAsync($"Couldn't handle Selection menu: {selection} ", ephemeral: true);
                    Serilog.Log.Error($"Couldn't handle Selection Menu: {selection}");
                    return;
            }

            // Example: Respond with the selected values
            await FollowupAsync($"You selected: {string.Join(", ", selectedValues)}", ephemeral: true);
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
        private async Task CreateNewGameNewsChannel(GameNewsAdvancedCommandSettings settings)
        {
            var guildID = Context.Guild.Id;
            var channelID = Context.Channel.Id;

            var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(guildID, channelID);

            if(leagueChannel != null)
            {
                await _discordRepo.SetLeagueGameNewsSetting(leagueChannel.LeagueID, guildID, channelID, true, NotableMissSetting.None);
                await _discordRepo.SetGameNewsSetting(guildID, channelID, settings.ToGameNewsSettings());
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

        private async Task UpdateGameNewsSettings(GameNewsAdvancedCommandSettings settings)
        {
            var leagueChannel = await _discordRepo.GetMinimalLeagueChannel(Context.Guild.Id, Context.Channel.Id);

            if (leagueChannel != null)
            {
                //await _discordRepo.SetLeagueGameNewsSetting(leagueChannel.LeagueID, leagueChannel.GuildID, leagueChannel.ChannelID);
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
        private ButtonBuilder GetEnableEligibleLeagueGameNewsOnlyButton(bool initialSetting)
        {
            return new ButtonBuilder()
                .WithCustomId("button_eligible_game_news")
                .WithLabel("Enable Eligible League Game News Only")
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
                .AddOption("NotableMissSetting.InitialScore", "InitialScore", isDefault: defaultSetting == NotableMissSetting.InitialScore)
                .AddOption("NotableMissSetting.ScoreUpdates", "ScoreUpdates", isDefault: defaultSetting == NotableMissSetting.ScoreUpdates)
                .AddOption("NotableMissSetting.None", "None", isDefault: defaultSetting == NotableMissSetting.None);
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
