using FantasyCritic.Lib.Discord;
using FantasyCritic.Lib.Domain.AllTimeStats;
using FantasyCritic.Lib.Domain.Calculations;
using FantasyCritic.Lib.Domain.Combinations;
using FantasyCritic.Lib.Domain.LeagueActions;
using FantasyCritic.Lib.Domain.Requests;
using FantasyCritic.Lib.Extensions;
using FantasyCritic.Lib.Identity;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.Lib.Utilities;

namespace FantasyCritic.Lib.Services;

public class FantasyCriticService
{
    private readonly IFantasyCriticRepo _fantasyCriticRepo;
    private readonly ICombinedDataRepo _combinedDataRepo;
    private readonly IDiscordRepo _discordRepo;
    private readonly IClock _clock;
    private readonly LeagueMemberService _leagueMemberService;
    private readonly InterLeagueService _interLeagueService;
    private readonly DiscordPushService _discordPushService;
    private readonly GameAcquisitionService _gameAcquisitionService;

    public FantasyCriticService(LeagueMemberService leagueMemberService, InterLeagueService interLeagueService, DiscordPushService discordPushService,
        GameAcquisitionService gameAcquisitionService, IFantasyCriticRepo fantasyCriticRepo, ICombinedDataRepo combinedDataRepo, IDiscordRepo discordRepo, IClock clock)
    {
        _fantasyCriticRepo = fantasyCriticRepo;
        _combinedDataRepo = combinedDataRepo;
        _discordRepo = discordRepo;
        _clock = clock;
        _leagueMemberService = leagueMemberService;
        _interLeagueService = interLeagueService;
        _discordPushService = discordPushService;
        _gameAcquisitionService = gameAcquisitionService;
    }

    public Task<HomePageData> GetHomePageData(FantasyCriticUser currentUser)
    {
        return _combinedDataRepo.GetHomePageData(currentUser);
    }

    public Task<League?> GetLeagueByID(Guid leagueID)
    {
        return _fantasyCriticRepo.GetLeague(leagueID);
    }

    public Task<LeagueYear?> GetLeagueYear(Guid leagueID, int year)
    {
        return _combinedDataRepo.GetLeagueYear(leagueID, year);
    }

    public Task<LeagueYearWithUserStatus?> GetLeagueYearWithUserStatus(Guid leagueID, int year)
    {
        return _combinedDataRepo.GetLeagueYearWithUserStatus(leagueID, year);
    }

    public async Task<LeagueYearWithSupplementalData?> GetLeagueYearWithSupplementalData(Guid leagueID, int year, FantasyCriticUser? currentUser)
    {
        var leagueYearWithSupplementalData = await _combinedDataRepo.GetLeagueYearWithSupplementalData(leagueID, year, currentUser);
        if (leagueYearWithSupplementalData is null)
        {
            return null;
        }

        var leagueYear = leagueYearWithSupplementalData.LeagueYear;
        var supplementalDataFromRepo = leagueYearWithSupplementalData.SupplementalData;

        var publicBiddingSet = _gameAcquisitionService.GetPublicBiddingGames(leagueYear, supplementalDataFromRepo.ActivePickupBids,
            supplementalDataFromRepo.ActiveSpecialAuctions, supplementalDataFromRepo.MasterGameYearDictionary);

        var supplementalData = new LeagueYearSupplementalData(supplementalDataFromRepo.SystemWideValues,
            supplementalDataFromRepo.ManagerMessages, supplementalDataFromRepo.PreviousYearWinnerUserID,
            supplementalDataFromRepo.ActiveTrades, supplementalDataFromRepo.ActiveSpecialAuctions, publicBiddingSet,
            supplementalDataFromRepo.UserIsFollowingLeague, supplementalDataFromRepo.AllPublishersForUser,
            supplementalDataFromRepo.PrivatePublisherData, supplementalDataFromRepo.MasterGameYearDictionary);

        return new LeagueYearWithSupplementalData(leagueYear, supplementalData, leagueYearWithSupplementalData.UserStatus);
    }

    public Task<IReadOnlyList<LeagueYear>> GetLeagueYears(int year)
    {
        return _fantasyCriticRepo.GetLeagueYears(year);
    }

    public Task<LeagueYearKey?> GetLeagueYearKeyForPublisherID(Guid publisherID)
    {
        return _fantasyCriticRepo.GetLeagueYearKeyForPublisherID(publisherID);
    }

    public async Task<Result<League>> CreateLeague(LeagueCreationParameters parameters)
    {
        LeagueOptions options = new LeagueOptions(parameters.LeagueYearParameters);

        var validateOptions = options.Validate();
        if (validateOptions.IsFailure)
        {
            return Result.Failure<League>(validateOptions.Error);
        }

        if (!parameters.LeagueYearParameters.ScoringSystem.SupportedInYear(parameters.LeagueYearParameters.Year))
        {
            return Result.Failure<League>("That scoring mode is no longer supported.");
        }

        IEnumerable<int> years = new List<int>() { parameters.LeagueYearParameters.Year };
        League newLeague = new League(Guid.NewGuid(), parameters.LeagueName, parameters.Manager.ToMinimal(), null, null, years, parameters.PublicLeague, parameters.TestLeague, parameters.CustomRulesLeague, false, 0);
        await _fantasyCriticRepo.CreateLeague(newLeague, parameters.LeagueYearParameters.Year, options);
        return Result.Success(newLeague);
    }

    public async Task<Result> EditLeague(LeagueYear leagueYear, LeagueYearParameters parameters)
    {
        if (leagueYear.SupportedYear.Finished)
        {
            return Result.Failure("You cannot edit a completed year.");
        }

        var league = leagueYear.League;
        LeagueOptions options = new LeagueOptions(parameters);
        var validateOptions = options.Validate();
        if (validateOptions.IsFailure)
        {
            return Result.Failure(validateOptions.Error);
        }

        if (!parameters.ScoringSystem.SupportedInYear(leagueYear.Year))
        {
            return Result.Failure("That scoring mode is no longer supported.");
        }

        IReadOnlyList<Publisher> publishers = leagueYear.Publishers;
        int maxStandardGames = publishers.Select(publisher => publisher.PublisherGames.Count(x => !x.CounterPick)).DefaultIfEmpty(0).Max();
        int maxCounterPicks = publishers.Select(publisher => publisher.PublisherGames.Count(x => x.CounterPick)).DefaultIfEmpty(0).Max();

        if (maxStandardGames > options.StandardGames)
        {
            return Result.Failure($"Cannot reduce number of standard games to {options.StandardGames} as a publisher has {maxStandardGames} standard games currently.");
        }
        if (maxCounterPicks > options.CounterPicks)
        {
            return Result.Failure($"Cannot reduce number of counter picks to {options.CounterPicks} as a publisher has {maxCounterPicks} counter picks currently.");
        }

        if (leagueYear.PlayStatus.DraftIsActive)
        {
            if (leagueYear.Options.GamesToDraft > parameters.GamesToDraft)
            {
                return Result.Failure("Cannot decrease the number of drafted games during the draft. Reset the draft if you need to do this.");
            }

            if (leagueYear.Options.CounterPicksToDraft > parameters.CounterPicksToDraft)
            {
                return Result.Failure("Cannot decrease the number of drafted counter picks during the draft. Reset the draft if you need to do this.");
            }
        }

        if (leagueYear.PlayStatus.DraftFinished)
        {
            if (leagueYear.Options.GamesToDraft != parameters.GamesToDraft)
            {
                return Result.Failure("Cannot change the number of drafted games after the draft.");
            }

            if (leagueYear.Options.CounterPicksToDraft != parameters.CounterPicksToDraft)
            {
                return Result.Failure("Cannot change the number of drafted counter picks after the draft.");
            }
        }

        int maxFreeGamesFreeDropped = publishers.Select(publisher => publisher.FreeGamesDropped).DefaultIfEmpty(0).Max();
        int maxWillNotReleaseGamesDropped = publishers.Select(publisher => publisher.WillNotReleaseGamesDropped).DefaultIfEmpty(0).Max();
        int maxWillReleaseGamesDropped = publishers.Select(publisher => publisher.WillReleaseGamesDropped).DefaultIfEmpty(0).Max();

        if (maxFreeGamesFreeDropped > options.FreeDroppableGames && options.FreeDroppableGames != -1)
        {
            return Result.Failure($"Cannot reduce number of unrestricted droppable games to {options.FreeDroppableGames} as a publisher has already dropped {maxFreeGamesFreeDropped} games.");
        }
        if (maxWillNotReleaseGamesDropped > options.WillNotReleaseDroppableGames && options.WillNotReleaseDroppableGames != -1)
        {
            return Result.Failure($"Cannot reduce number of 'will not release' droppable games to {options.WillNotReleaseDroppableGames} as a publisher has already dropped {maxWillNotReleaseGamesDropped} games.");
        }
        if (maxWillReleaseGamesDropped > options.WillReleaseDroppableGames && options.WillReleaseDroppableGames != -1)
        {
            return Result.Failure($"Cannot reduce number of 'will release' droppable games to {options.WillReleaseDroppableGames} as a publisher has already dropped {maxWillReleaseGamesDropped} games.");
        }

        var slotAssignments = GetNewSlotAssignments(parameters, leagueYear, publishers);
        var eligibilityOverrides = leagueYear.EligibilityOverrides;
        var tagOverrides = leagueYear.TagOverrides;
        var supportedYear = await _interLeagueService.GetSupportedYear(parameters.Year);

        LeagueYear newLeagueYear = new LeagueYear(league, supportedYear, options,
            leagueYear.PlayStatus, leagueYear.DraftOrderSet, eligibilityOverrides,
            tagOverrides, leagueYear.DraftStartedTimestamp, leagueYear.WinningUser, publishers, leagueYear.ConferenceLocked);

        var differenceString = options.GetDifferenceString(leagueYear.Options);
        if (differenceString is not null)
        {
            LeagueManagerAction settingsChangeAction = new LeagueManagerAction(leagueYear.Key, _clock.GetCurrentInstant(), "League Year Settings Changed", differenceString);
            await _fantasyCriticRepo.EditLeagueYear(newLeagueYear, slotAssignments, settingsChangeAction);
            await _discordPushService.SendLeagueActionMessage(settingsChangeAction);
        }

        return Result.Success();
    }

    private static IReadOnlyDictionary<Guid, int> GetNewSlotAssignments(LeagueYearParameters parameters, LeagueYear leagueYear, IReadOnlyList<Publisher> publishers)
    {
        var slotCountShift = parameters.StandardGames - leagueYear.Options.StandardGames;
        Dictionary<Guid, int> finalSlotAssignments = new Dictionary<Guid, int>();
        if (slotCountShift == 0)
        {
            return finalSlotAssignments;
        }

        foreach (var publisher in publishers)
        {
            Dictionary<Guid, int> slotAssignmentsForPublisher = new Dictionary<Guid, int>();
            var slots = publisher.GetPublisherSlots(leagueYear);
            var filledNonCounterPickSlots = slots.Where(x => !x.CounterPick && x.PublisherGame is not null).ToList();

            int normalSlotNumber = 0;
            var normalSlots = filledNonCounterPickSlots.Where(x => x.SpecialGameSlot is null);
            foreach (var normalSlot in normalSlots)
            {
                slotAssignmentsForPublisher[normalSlot.PublisherGame!.PublisherGameID] = normalSlotNumber;
                normalSlotNumber++;
            }

            var specialSlots = filledNonCounterPickSlots.Where(x => x.SpecialGameSlot is not null);
            foreach (var specialSlot in specialSlots)
            {
                slotAssignmentsForPublisher[specialSlot.PublisherGame!.PublisherGameID] =
                    specialSlot.SlotNumber + slotCountShift;
            }

            bool invalidSlotsMade = slotAssignmentsForPublisher.GroupBy(x => x.Value).Any(x => x.Count() > 1);
            if (invalidSlotsMade)
            {
                //If we cannot do the more advance way to preserve slots, then just do the very basic thing, and line the games up.
                slotAssignmentsForPublisher = new Dictionary<Guid, int>();
                int allSlotNumber = 0;
                foreach (var slot in filledNonCounterPickSlots)
                {
                    slotAssignmentsForPublisher[slot.PublisherGame!.PublisherGameID] = allSlotNumber;
                    allSlotNumber++;
                }
            }

            foreach (var slot in slotAssignmentsForPublisher)
            {
                finalSlotAssignments[slot.Key] = slot.Value;
            }
        }

        return finalSlotAssignments;
    }

    public async Task AddNewLeagueYear(League league, int year, LeagueOptions options, LeagueYear mostRecentLeagueYear)
    {
        var mostRecentActivePlayers = await _fantasyCriticRepo.GetActivePlayersForLeagueYear(league.LeagueID, mostRecentLeagueYear.Year);
        await _fantasyCriticRepo.AddNewLeagueYear(league, year, options, mostRecentActivePlayers);
    }

    public YearCalculatedStatsSet GetCalculatedStatsForYear(int year, IReadOnlyList<LeagueYear> leagueYears, bool recalculateWinners)
    {
        Dictionary<Guid, PublisherGameCalculatedStats> publisherGameCalculatedStats = new Dictionary<Guid, PublisherGameCalculatedStats>();
        IReadOnlyList<Publisher> allPublishersForYear = leagueYears.SelectMany(x => x.Publishers).ToList();
        var leagueYearDictionary = leagueYears.ToDictionary(x => x.Key);

        var currentDate = _clock.GetToday();
        Dictionary<LeagueYearKey, FantasyCriticUser> winningUsers = new Dictionary<LeagueYearKey, FantasyCriticUser>();
        var publishersByLeagueYear = allPublishersForYear.GroupBy(x => x.LeagueYearKey);
        foreach (var publishersForLeagueYear in publishersByLeagueYear)
        {
            var sortedLeagueYearPublishers = publishersForLeagueYear.OrderBy(x => x.DraftPosition).ToList();
            decimal highestPoints = 0m;
            var leagueYear = leagueYearDictionary[publishersForLeagueYear.Key];
            foreach (var publisher in sortedLeagueYearPublishers)
            {
                var slots = publisher.GetPublisherSlots(leagueYear).Where(x => x.PublisherGame is not null).ToList();
                foreach (var publisherSlot in slots)
                {
                    //Before 2022, games that were 'ineligible' still gave points. It was just a warning.
                    var ineligiblePointsShouldCount = !SupportedYear.Year2022FeatureSupported(year);
                    var gameIsEligible = publisherSlot.SlotIsValid(leagueYear);
                    var pointsShouldCount = gameIsEligible || ineligiblePointsShouldCount;

                    decimal? fantasyPoints = publisherSlot.GetFantasyPoints(pointsShouldCount, leagueYear.Options.ReleaseSystem, leagueYear.Options.ScoringSystem, currentDate);
                    var stats = new PublisherGameCalculatedStats(fantasyPoints);
                    publisherGameCalculatedStats.Add(publisherSlot.PublisherGame!.PublisherGameID, stats);
                }

                decimal totalPointsForPublisher = publisher.GetTotalFantasyPoints(leagueYear.SupportedYear, leagueYear.Options);
                if (totalPointsForPublisher >= highestPoints && (leagueYear.WinningUser is null || recalculateWinners))
                {
                    highestPoints = totalPointsForPublisher;
                    winningUsers[publisher.LeagueYearKey] = publisher.User;
                }
            }
        }

        return new YearCalculatedStatsSet(publisherGameCalculatedStats, winningUsers);
    }



    public async Task UpdatePublisherGameCalculatedStats(LeagueYear leagueYear)
    {
        Dictionary<Guid, PublisherGameCalculatedStats> calculatedStats = new Dictionary<Guid, PublisherGameCalculatedStats>();

        var currentDate = _clock.GetToday();
        foreach (var publisher in leagueYear.Publishers)
        {
            var slots = publisher.GetPublisherSlots(leagueYear);
            var slotsThatHaveGames = slots.Where(x => x.PublisherGame is not null).ToList();
            foreach (var publisherSlot in slotsThatHaveGames)
            {
                //Before 2022, games that were 'ineligible' still gave points. It was just a warning.
                var ineligiblePointsShouldCount = !SupportedYear.Year2022FeatureSupported(leagueYear.Year);
                var gameIsEligible = publisherSlot.SlotIsValid(leagueYear);
                var pointsShouldCount = gameIsEligible || ineligiblePointsShouldCount;

                decimal? fantasyPoints = publisherSlot.GetFantasyPoints(pointsShouldCount, leagueYear.Options.ReleaseSystem, leagueYear.Options.ScoringSystem, currentDate);
                var stats = new PublisherGameCalculatedStats(fantasyPoints);
                calculatedStats.Add(publisherSlot.PublisherGame!.PublisherGameID, stats);
            }
        }

        await _fantasyCriticRepo.UpdatePublisherGameCalculatedStats(calculatedStats);

        var newLeagueYear = leagueYear.GetUpdatedLeagueYearWithNewScores(calculatedStats);
        var scoreChanges = new LeagueYearScoreChanges(leagueYear, newLeagueYear);
        await _discordPushService.SendLeagueYearScoreUpdateMessage(scoreChanges);
    }

    public Task ManuallyScoreGame(PublisherGame publisherGame, decimal? manualCriticScore)
    {
        return _fantasyCriticRepo.ManuallyScoreGame(publisherGame, manualCriticScore);
    }

    public async Task ManuallySetWillNotRelease(LeagueYear leagueYear, PublisherGame publisherGame, bool willNotRelease)
    {
        await _fantasyCriticRepo.ManuallySetWillNotRelease(publisherGame, willNotRelease);
        string description;
        if (willNotRelease)
        {
            description = $"{publisherGame.GameName}' was manually set as 'Will Not Release'.";
        }
        else
        {
            description = $"{publisherGame.GameName}'s manual 'Will Not Release' setting was cleared.";
        }

        LeagueManagerAction eligibilityAction = new LeagueManagerAction(leagueYear.Key, _clock.GetCurrentInstant(), "'Will not release' Overridden", description);
        await _fantasyCriticRepo.AddLeagueManagerAction(eligibilityAction);
    }

    public Task<IReadOnlyList<LeagueAction>> GetLeagueActions(LeagueYear leagueYear)
    {
        return _fantasyCriticRepo.GetLeagueActions(leagueYear);
    }

    public Task<IReadOnlyList<LeagueManagerAction>> GetLeagueManagerActions(LeagueYear leagueYear)
    {
        return _fantasyCriticRepo.GetLeagueManagerActions(leagueYear);
    }

    public async Task<IReadOnlyList<LeagueActionProcessingSet>> GetLeagueActionProcessingSets(LeagueYear leagueYear)
    {
        var processSets = await _interLeagueService.GetActionProcessingSets();
        var bidsForLeague = await _fantasyCriticRepo.GetProcessedPickupBids(leagueYear);
        var dropsForLeague = await _fantasyCriticRepo.GetProcessedDropRequests(leagueYear);
        var bidsByProcessSet = bidsForLeague.ToLookup(x => x.ProcessSetID);
        var dropsByProcessSet = dropsForLeague.ToLookup(x => x.ProcessSetID);

        List<LeagueActionProcessingSet> processingSets = new List<LeagueActionProcessingSet>();
        foreach (var processSet in processSets)
        {
            var bids = bidsByProcessSet[processSet.ProcessSetID];
            var drops = dropsByProcessSet[processSet.ProcessSetID];
            processingSets.Add(new LeagueActionProcessingSet(leagueYear, processSet.ProcessSetID, processSet.ProcessTime, processSet.ProcessName, drops, bids));
        }

        return processingSets;
    }

    public async Task ChangeLeagueOptions(League league, string leagueName, bool publicLeague, bool testLeague, bool customRulesLeague)
    {
        if (!publicLeague && league.PublicLeague)
        {
            await _discordRepo.RemoveAllLeagueChannelsForLeague(league.LeagueID);
        }

        await _fantasyCriticRepo.ChangeLeagueOptions(league, leagueName, publicLeague, testLeague, customRulesLeague);
    }

    public async Task DeleteLeague(League league)
    {
        var invites = await _fantasyCriticRepo.GetOutstandingInvitees(league);
        foreach (var invite in invites)
        {
            await _fantasyCriticRepo.DeleteInvite(invite);
        }

        foreach (var year in league.Years)
        {
            var leagueYear = await _combinedDataRepo.GetLeagueYearOrThrow(league.LeagueID, year);
            var publishers = leagueYear.Publishers;
            foreach (var publisher in publishers)
            {
                await _fantasyCriticRepo.DeleteLeagueActions(publisher);
                foreach (var game in publisher.PublisherGames)
                {
                    await _fantasyCriticRepo.FullyRemovePublisherGame(leagueYear, publisher, game);
                }

                var bids = await _fantasyCriticRepo.GetActivePickupBids(leagueYear, publisher);
                foreach (var bid in bids)
                {
                    await _fantasyCriticRepo.RemovePickupBid(bid);
                }

                var dropRequests = await _fantasyCriticRepo.GetActiveDropRequests(leagueYear, publisher);
                foreach (var dropRequest in dropRequests)
                {
                    await _fantasyCriticRepo.RemoveDropRequest(dropRequest);
                }

                await _fantasyCriticRepo.DeletePublisher(publisher);
            }

            await _fantasyCriticRepo.DeleteLeagueYear(leagueYear);
        }

        var users = await _fantasyCriticRepo.GetUsersInLeague(league.LeagueID);
        foreach (var user in users)
        {
            await _fantasyCriticRepo.RemovePlayerFromLeague(league, user);
        }

        await _fantasyCriticRepo.DeleteLeague(league);
    }

    public Task<IReadOnlyList<FantasyCriticUser>> GetLeagueFollowers(League league)
    {
        return _fantasyCriticRepo.GetLeagueFollowers(league);
    }

    public async Task<Result> FollowLeague(League league, FantasyCriticUser user)
    {
        if (!league.PublicLeague)
        {
            return Result.Failure("League is not public");
        }

        bool userIsInLeague = await _leagueMemberService.UserIsInLeague(league, user);
        if (userIsInLeague)
        {
            return Result.Failure("Can't follow a league you are in.");
        }

        var leaguesForUser = await _fantasyCriticRepo.GetLeaguesForUser(user);
        bool userIsFollowingLeague = leaguesForUser.Any(x => x.UserIsFollowingLeague && x.League.LeagueID == league.LeagueID);
        if (userIsFollowingLeague)
        {
            return Result.Failure("User is already following that league.");
        }

        await _fantasyCriticRepo.FollowLeague(league, user);
        return Result.Success();
    }

    public async Task<Result> UnfollowLeague(League league, FantasyCriticUser user)
    {
        var leaguesForUser = await _fantasyCriticRepo.GetLeaguesForUser(user);
        bool userIsFollowingLeague = leaguesForUser.Any(x => x.UserIsFollowingLeague && x.League.LeagueID == league.LeagueID);
        if (!userIsFollowingLeague)
        {
            return Result.Failure("User is not following that league.");
        }

        await _fantasyCriticRepo.UnfollowLeague(league, user);
        return Result.Success();
    }

    public Task<IReadOnlyList<PublicLeagueYearStats>> GetPublicLeagueYears(int year, int? count)
    {
        return _fantasyCriticRepo.GetPublicLeagueYears(year, count);
    }

    public async Task SetEligibilityOverride(LeagueYear leagueYear, MasterGame masterGame, bool? eligible)
    {
        if (!eligible.HasValue)
        {
            await _fantasyCriticRepo.DeleteEligibilityOverride(leagueYear, masterGame);
        }
        else
        {
            await _fantasyCriticRepo.SetEligibilityOverride(leagueYear, masterGame, eligible.Value);
        }

        string description;
        if (!eligible.HasValue)
        {
            description = $"{masterGame.GameName}'s eligibility setting was reset to normal.";
        }
        else if (eligible.Value)
        {
            description = $"{masterGame.GameName} was manually set to 'Eligible'";
        }
        else
        {
            description = $"{masterGame.GameName} was manually set to 'Ineligible'";
        }

        LeagueManagerAction eligibilityAction = new LeagueManagerAction(leagueYear.Key, _clock.GetCurrentInstant(), "Eligibility Setting Changed", description);
        await _fantasyCriticRepo.AddLeagueManagerAction(eligibilityAction);
    }

    public async Task SetTagOverride(LeagueYear leagueYear, MasterGame masterGame, List<MasterGameTag> requestedTags)
    {
        await _fantasyCriticRepo.SetTagOverride(leagueYear, masterGame, requestedTags);
        if (requestedTags.Any())
        {
            var tagNames = string.Join(", ", requestedTags.Select(x => x.ReadableName));
            string description = $"{masterGame.GameName}'s tags were set to: '{tagNames}'.";
            LeagueManagerAction tagAction = new LeagueManagerAction(leagueYear.Key, _clock.GetCurrentInstant(), "Tags Overriden", description);
            await _fantasyCriticRepo.AddLeagueManagerAction(tagAction);
        }
        else
        {
            string description = $"{masterGame.GameName}'s tags were reset to default.";
            LeagueManagerAction tagAction = new LeagueManagerAction(leagueYear.Key, _clock.GetCurrentInstant(), "Tags Override Cleared", description);
            await _fantasyCriticRepo.AddLeagueManagerAction(tagAction);
        }
    }

    public async Task PostNewManagerMessage(LeagueYear leagueYear, string message, bool isPublic)
    {
        var domainMessage = new ManagerMessage(Guid.NewGuid(), message, isPublic, _clock.GetCurrentInstant(), new List<Guid>());
        await _fantasyCriticRepo.PostNewManagerMessage(leagueYear, domainMessage);
        await _discordPushService.SendLeagueManagerAnnouncementMessage(leagueYear, message);
    }

    public Task<Result> DeleteManagerMessage(LeagueYear leagueYear, Guid messageID)
    {
        return _fantasyCriticRepo.DeleteManagerMessage(leagueYear, messageID);
    }

    public Task<Result> DismissManagerMessage(Guid messageID, Guid userID)
    {
        return _fantasyCriticRepo.DismissManagerMessage(messageID, userID);
    }

    public Task<bool> DraftIsActiveOrPaused(Guid leagueID, int year)
    {
        return _fantasyCriticRepo.DraftIsActiveOrPaused(leagueID, year);
    }

    public async Task<LeagueAllTimeStats> GetLeagueAllTimeStats(League league, LocalDate currentDate)
    {
        var leagueYears = new List<LeagueYear>();
        foreach (var year in league.Years)
        {
            var leagueYear = await GetLeagueYear(league.LeagueID, year);
            if (leagueYear is not null && leagueYear.SupportedYear.Finished)
            {
                leagueYears.Add(leagueYear);
            }
        }

        var leagueYearDictionary = leagueYears.ToDictionary(x => x.Key);
        var playerAllTimeStats = new List<LeaguePlayerAllTimeStats>();
        var groupedByPlayer = leagueYears.SelectMany(x => x.Publishers).GroupBy(x => x.User);
        foreach (var playerGroup in groupedByPlayer)
        {
            var player = new VeryMinimalFantasyCriticUser(playerGroup.Key.UserID, playerGroup.Key.DisplayName);
            var yearsPlayedIn = playerGroup.Count();
            var yearsWon = leagueYears.Where(x => x.WinningUser is not null && x.WinningUser.UserID == player.UserID).Select(x => x.Year).ToList();

            var totalFantasyPoints = playerGroup.Sum(x => x.GetTotalFantasyPoints(leagueYearDictionary[x.LeagueYearKey].SupportedYear, leagueYearDictionary[x.LeagueYearKey].Options));
            var allPublisherGames = playerGroup.SelectMany(x => x.PublisherGames).ToList();
            var gamesReleased = allPublisherGames
                .Where(x => !x.CounterPick)
                .Where(x => x.MasterGame is not null)
                .Where(x => x.MasterGame!.MasterGame.IsReleased(currentDate))
                .ToList();

            var timesCounterPicked = 0;
            var allFinishRankings = new List<int>();
            foreach (var publisher in playerGroup)
            {
                var leagueYear = leagueYearDictionary[publisher.LeagueYearKey];
                var ranking = leagueYear.Publishers.Count(y => y.GetTotalFantasyPoints(leagueYear.SupportedYear, leagueYear.Options) >
                                                               publisher.GetTotalFantasyPoints(leagueYear.SupportedYear, leagueYear.Options)) + 1;
                allFinishRankings.Add(ranking);

                var timesCounterPickedInYear = GameUtilities.GetTimesCounterPicked(leagueYear, publisher);
                timesCounterPicked += timesCounterPickedInYear;
            }

            var averageFinishRanking = allFinishRankings.Average();
            var averageGamesReleased = (double)gamesReleased.Count / yearsPlayedIn;
            var averageFantasyPoints = totalFantasyPoints / yearsPlayedIn;
            var averageCriticScore = gamesReleased.Where(x => x.MasterGame!.MasterGame.CriticScore.HasValue).Select(x => x.MasterGame!.MasterGame.CriticScore).Average() ?? 0m;
            

            playerAllTimeStats.Add(new LeaguePlayerAllTimeStats(player, yearsPlayedIn, yearsWon, totalFantasyPoints, gamesReleased.Count,
                averageFinishRanking, averageGamesReleased, averageFantasyPoints, averageCriticScore, timesCounterPicked));
        }

        var systemWideValues = await _interLeagueService.GetSystemWideValues();
        var hallOfFameGameLists = GetHallOfFameLists(leagueYears, leagueYearDictionary, systemWideValues, currentDate);

        return new LeagueAllTimeStats(leagueYears, playerAllTimeStats, hallOfFameGameLists);
    }

    private static IReadOnlyList<HallOfFameGameList> GetHallOfFameLists(IReadOnlyList<LeagueYear> leagueYears, IReadOnlyDictionary<LeagueYearKey, LeagueYear> leagueYearDictionary,
        SystemWideValues systemWideValues, LocalDate currentDate)
    {
        var publisherDictionary = leagueYears.SelectMany(x => x.Publishers).ToDictionary(x => x.PublisherID);

        var publisherGamesWithMasterGames = leagueYears
            .SelectMany(x => x.Publishers)
            .SelectMany(x => x.PublisherGames)
            .Where(x => x.MasterGame is not null)
            .ToList();
        var publisherGamesWithScores = publisherGamesWithMasterGames
            .Where(x => x.MasterGame is not null && x.FantasyPoints.HasValue && x.MasterGame!.MasterGame.CriticScore.HasValue)
            .ToList();

        //Build Hall of Fame
        var mostPoints = publisherGamesWithScores
            .Where(x => !x.CounterPick)
            .OrderByDescending(x => x.FantasyPoints)
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Critic Score", new HallOfFameGameStat(x.MasterGame!.IsReleasedAndReleasedInYear(currentDate) ? x.MasterGame!.MasterGame.CriticScore!.Value : "DNR", "Score")},
                {"Fantasy Points", new HallOfFameGameStat(x.FantasyPoints ?? 0, "Score") }
            }))
            .ToList();

        var leastPoints = publisherGamesWithScores
            .Where(x => !x.CounterPick)
            .OrderBy(x => x.FantasyPoints)
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Critic Score", new HallOfFameGameStat(x.MasterGame !.IsReleasedAndReleasedInYear(currentDate) ? x.MasterGame !.MasterGame.CriticScore !.Value : "DNR", "Score")},
                {"Fantasy Points", new HallOfFameGameStat(x.FantasyPoints ?? 0, "Score") }
            }))
            .ToList();

        var bestCounterPicks = publisherGamesWithMasterGames
            .Where(x => x.CounterPick)
            .OrderByDescending(x => x.FantasyPoints)
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Critic Score", new HallOfFameGameStat(x.MasterGame !.IsReleasedAndReleasedInYear(currentDate) ? x.MasterGame !.MasterGame.CriticScore !.Value : "DNR", "Score")},
                {"Fantasy Points", new HallOfFameGameStat(x.FantasyPoints ?? 0, "Score") }
            }))
            .ToList();

        var worstCounterPicks = publisherGamesWithScores
            .Where(x => x.CounterPick)
            .OrderBy(x => x.FantasyPoints)
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Critic Score", new HallOfFameGameStat(x.MasterGame !.IsReleasedAndReleasedInYear(currentDate) ? x.MasterGame !.MasterGame.CriticScore !.Value : "DNR", "Score")},
                {"Fantasy Points", new HallOfFameGameStat(x.FantasyPoints ?? 0, "Score") }
            }))
            .ToList();

        var mostDollarsSpentOn = publisherGamesWithMasterGames
            .Where(x => x.BidAmount.HasValue)
            .OrderByDescending(x => x.BidAmount)
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Spent", new HallOfFameGameStat(x.BidAmount!.Value, "Budget")}
            }))
            .ToList();

        var highestSleeperFactor = publisherGamesWithScores
            .Where(x => !x.CounterPick && publisherDictionary[x.PublisherID].LeagueYearKey.Year > 2018)
            .OrderByDescending(x => x.GetSleeperFactor(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem))
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Hype Factor", new HallOfFameGameStat(x.MasterGame !.DateAdjustedHypeFactor, "Factor")},
                {"Critic Score", new HallOfFameGameStat(x.MasterGame !.IsReleasedAndReleasedInYear(currentDate) ? x.MasterGame !.MasterGame.CriticScore !.Value : "DNR", "Score")},
                {"Sleeper Factor", new HallOfFameGameStat(x.GetSleeperFactor(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem), "Factor")}
            }))
            .ToList();

        var highestFlopFactor = publisherGamesWithScores
            .Where(x => !x.CounterPick && publisherDictionary[x.PublisherID].LeagueYearKey.Year > 2018)
            .OrderByDescending(x => x.GetFlopFactor(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem))
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Hype Factor", new HallOfFameGameStat(x.MasterGame !.DateAdjustedHypeFactor, "Factor")},
                {"Critic Score", new HallOfFameGameStat(x.MasterGame !.IsReleasedAndReleasedInYear(currentDate) ? x.MasterGame !.MasterGame.CriticScore !.Value : "DNR", "Score")},
                {"Flop Factor",  new HallOfFameGameStat(x.GetFlopFactor(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem), "Factor")}
            }))
            .ToList();

        var highestDraftValue = publisherGamesWithScores
            .Where(x => !x.CounterPick && x.OverallDraftPosition.HasValue)
            .OrderByDescending(x => x.GetDraftValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem))
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Drafted", new HallOfFameGameStat(x.OverallDraftPosition !.Value, "Position")},
                {"Critic Score", new HallOfFameGameStat(x.MasterGame!.MasterGame.CriticScore!.Value, "Score")},
                {"Draft Value",  new HallOfFameGameStat(x.GetDraftValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem), "Factor")}
            }))
            .ToList();

        var lowestDraftValue = publisherGamesWithMasterGames
            .Where(x => !x.CounterPick && x.OverallDraftPosition.HasValue)
            .OrderBy(x => x.GetDraftValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem))
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Drafted", new HallOfFameGameStat(x.OverallDraftPosition !.Value, "Position")},
                {"Critic Score", new HallOfFameGameStat(x.MasterGame !.MasterGame.CriticScore.HasValue ? x.MasterGame !.MasterGame.CriticScore.Value : "DNR", "Score")},
                {"Draft Value",  new HallOfFameGameStat(x.GetDraftValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem), "Factor")}
            }))
            .ToList();

        var highestBidValue = publisherGamesWithScores
            .Where(x => !x.CounterPick && x.BidAmount.HasValue && publisherDictionary[x.PublisherID].LeagueYearKey.Year > 2018)
            .OrderByDescending(x => x.GetBidValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem))
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Spent", new HallOfFameGameStat(x.BidAmount !.Value, "Budget")},
                {"Critic Score", new HallOfFameGameStat(x.MasterGame!.MasterGame.CriticScore!.Value, "Score")},
                {"Bid Value",  new HallOfFameGameStat(x.GetBidValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem), "Factor")}
            }))
            .ToList();

        var lowestBidValue = publisherGamesWithMasterGames
            .Where(x => !x.CounterPick && x.BidAmount.HasValue && publisherDictionary[x.PublisherID].LeagueYearKey.Year > 2018)
            .OrderBy(x => x.GetBidValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem))
            .Take(10)
            .Select(x => new HallOfFameGame(x.MasterGame!, publisherDictionary[x.PublisherID], new Dictionary<string, HallOfFameGameStat>()
            {
                {"Spent",  new HallOfFameGameStat(x.BidAmount !.Value, "Budget")},
                {"Critic Score", new HallOfFameGameStat(x.MasterGame !.MasterGame.CriticScore.HasValue ? x.MasterGame !.MasterGame.CriticScore.Value : "DNR", "Score")},
                {"Bid Value",  new HallOfFameGameStat(x.GetBidValue(leagueYearDictionary[publisherDictionary[x.PublisherID].LeagueYearKey].Options.ScoringSystem), "Factor")}
            }))
            .ToList();

        var hallOfFameGameLists = new List<HallOfFameGameList>()
        {
            new HallOfFameGameList("Highest Scoring Games", mostPoints),
            new HallOfFameGameList("Lowest Scoring Games", leastPoints),
            new HallOfFameGameList("Best Counter Picks", bestCounterPicks),
            new HallOfFameGameList("Worst Counter Picks", worstCounterPicks),
            new HallOfFameGameList("Most Budget Spent On", mostDollarsSpentOn),
            new HallOfFameGameList("Sleeper Factor", highestSleeperFactor),
            new HallOfFameGameList("Flop Factor", highestFlopFactor),

            new HallOfFameGameList("Highest Value Draft Picks", highestDraftValue),
            new HallOfFameGameList("Lowest Value Draft Picks", lowestDraftValue),
            new HallOfFameGameList("Highest Value Pickups", highestBidValue),
            new HallOfFameGameList("Lowest Value Pickups", lowestBidValue),
        };
        return hallOfFameGameLists;
    }
}
