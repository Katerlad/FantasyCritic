using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FantasyCritic.Lib.Domain;
using FantasyCritic.Lib.Domain.Requests;
using FantasyCritic.Lib.Domain.Results;
using FantasyCritic.Lib.Domain.ScoringSystems;
using FantasyCritic.Lib.Enums;
using FantasyCritic.Lib.Extensions;
using FantasyCritic.Lib.Identity;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.Lib.Services;
using FantasyCritic.Web.Extensions;
using FantasyCritic.Web.Hubs;
using FantasyCritic.Web.Models;
using FantasyCritic.Web.Models.Requests;
using FantasyCritic.Web.Models.Requests.League;
using FantasyCritic.Web.Models.Requests.LeagueManager;
using FantasyCritic.Web.Models.Requests.Shared;
using FantasyCritic.Web.Models.Responses;
using FantasyCritic.Web.Models.RoundTrip;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NodaTime;

namespace FantasyCritic.Web.Controllers.API
{
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class LeagueManagerController : FantasyCriticController
    {
        private readonly FantasyCriticUserManager _userManager;
        private readonly FantasyCriticService _fantasyCriticService;
        private readonly InterLeagueService _interLeagueService;
        private readonly LeagueMemberService _leagueMemberService;
        private readonly DraftService _draftService;
        private readonly PublisherService _publisherService;
        private readonly IClock _clock;
        private readonly IHubContext<UpdateHub> _hubContext;
        private readonly IEmailSender _emailSender;
        private readonly GameAcquisitionService _gameAcquisitionService;

        public LeagueManagerController(FantasyCriticUserManager userManager, FantasyCriticService fantasyCriticService, InterLeagueService interLeagueService,
            LeagueMemberService leagueMemberService, DraftService draftService, PublisherService publisherService, IClock clock, IHubContext<UpdateHub> hubContext,
            IEmailSender emailSender, GameAcquisitionService gameAcquisitionService) : base(userManager)
        {
            _userManager = userManager;
            _fantasyCriticService = fantasyCriticService;
            _interLeagueService = interLeagueService;
            _leagueMemberService = leagueMemberService;
            _draftService = draftService;
            _publisherService = publisherService;
            _clock = clock;
            _hubContext = hubContext;
            _emailSender = emailSender;
            _gameAcquisitionService = gameAcquisitionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLeague([FromBody] CreateLeagueRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest("Some of the settings you chose are not valid.");
            }

            if (request.Tags.Required.Any())
            {
                return BadRequest("Impressive API usage, but required tags are not ready for prime time yet.");
            }

            if (!SupportedYear.Year2022FeatureSupported(request.InitialYear) && request.CounterPicks != request.CounterPicksToDraft)
            {
                return BadRequest("Pickup counter picks are not supported until 2022.");
            }

            var supportedYears = await _interLeagueService.GetSupportedYears();
            var selectedSupportedYear = supportedYears.SingleOrDefault(x => x.Year == request.InitialYear);
            if (selectedSupportedYear is null)
            {
                return BadRequest();
            }

            var userIsBetaUser = await _userManager.IsInRoleAsync(currentUser, "BetaTester");
            bool yearIsOpen = selectedSupportedYear.OpenForCreation || (userIsBetaUser && selectedSupportedYear.OpenForBetaUsers);
            if (!yearIsOpen)
            {
                return BadRequest();
            }

            var tagDictionary = await _interLeagueService.GetMasterGameTagDictionary();
            LeagueCreationParameters domainRequest = request.ToDomain(currentUser, tagDictionary);
            var league = await _fantasyCriticService.CreateLeague(domainRequest);
            if (league.IsFailure)
            {
                return BadRequest(league.Error);
            }

            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> AvailableYears(Guid id)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(id);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            IReadOnlyList<SupportedYear> supportedYears = await _interLeagueService.GetSupportedYears();
            var openYears = supportedYears.Where(x => x.OpenForCreation).Select(x => x.Year);
            var availableYears = openYears.Except(league.Value.Years);

            var userIsBetaUser = await _userManager.IsInRoleAsync(currentUser, "BetaTester");
            if (userIsBetaUser)
            {
                var betaYears = supportedYears.Where(x => x.OpenForBetaUsers).Select(x => x.Year);
                availableYears = availableYears.Concat(betaYears).Distinct();
            }

            return Ok(availableYears);
        }

        [HttpPost]
        public async Task<IActionResult> AddNewLeagueYear([FromBody] NewLeagueYearRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            if (league.Value.Years.Contains(request.Year))
            {
                return BadRequest();
            }

            var supportedYears = await _interLeagueService.GetSupportedYears();
            var selectedSupportedYear = supportedYears.SingleOrDefault(x => x.Year == request.Year);
            if (selectedSupportedYear is null)
            {
                return BadRequest();
            }

            var userIsBetaUser = await _userManager.IsInRoleAsync(currentUser, "BetaTester");
            bool yearIsOpen = selectedSupportedYear.OpenForCreation || (userIsBetaUser && selectedSupportedYear.OpenForBetaUsers);
            if (!yearIsOpen)
            {
                return BadRequest();
            }

            if (!league.Value.Years.Any())
            {
                throw new Exception("League has no initial year.");
            }

            var mostRecentYear = league.Value.Years.Max();
            var mostRecentLeagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, mostRecentYear);
            if (mostRecentLeagueYear.HasNoValue)
            {
                throw new Exception("Most recent league year could not be found");
            }

            await _fantasyCriticService.AddNewLeagueYear(league.Value, request.Year, mostRecentLeagueYear.Value.Options, mostRecentLeagueYear.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeLeagueOptions([FromBody] ChangeLeagueOptionsRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            if (league.Value.TestLeague)
            {
                //Users can't change a test league to a non test.
                request.TestLeague = true;
            }

            await _fantasyCriticService.ChangeLeagueOptions(league.Value, request.LeagueName, request.PublicLeague, request.TestLeague);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> EditLeagueYearSettings([FromBody] LeagueYearSettingsViewModel request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (request.Tags.Required.Any())
            {
                return BadRequest("Impressive API usage, but required tags are not ready for prime time yet.");
            }

            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            if (!SupportedYear.Year2022FeatureSupported(leagueYear.Value.Year))
            {
                if (request.CounterPicks != request.CounterPicksToDraft)
                {
                    return BadRequest("Pickup counter picks are not supported until 2022.");
                }
                if (request.PickupSystem != PickupSystem.SecretBidding.Value)
                {
                    return BadRequest("That bidding System is not supported until 2022.");
                }
                if (request.SpecialGameSlots.Any())
                {
                    return BadRequest("Special Game Slots are not supported until 2022.");
                }
            }

            if (request.SpecialGameSlots.Count > request.StandardGames)
            {
                return BadRequest("You cannot have more special game slots than the number of games per player.");
            }

            if (request.SpecialGameSlots.Any(x => !x.RequiredTags.Any()))
            {
                return BadRequest("All of your special slots must list at least one tag.");
            }

            var tagDictionary = await _interLeagueService.GetMasterGameTagDictionary();
            EditLeagueYearParameters domainRequest = request.ToDomain(currentUser, tagDictionary);
            Result result = await _fantasyCriticService.EditLeague(league.Value, domainRequest);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            await _fantasyCriticService.UpdatePublisherGameCalculatedStats(leagueYear.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> InvitePlayer([FromBody] CreateInviteRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            string baseURL = $"{Request.Scheme}://{Request.Host.Value}";
            FantasyCriticUser inviteUser;
            if (!request.IsDisplayNameInvite())
            {
                string inviteEmail = request.InviteEmail.ToLower();
                inviteUser = await _userManager.FindByEmailAsync(inviteEmail);
                if (inviteUser is null)
                {
                    Result userlessInviteResult = await _leagueMemberService.InviteUserByEmail(league.Value, inviteEmail);
                    if (userlessInviteResult.IsFailure)
                    {
                        return BadRequest(userlessInviteResult.Error);
                    }

                    await _emailSender.SendSiteInviteEmail(inviteEmail, league.Value, baseURL);
                    return Ok();
                }
            }
            else
            {
                inviteUser = await _userManager.FindByDisplayName(request.InviteDisplayName, request.InviteDisplayNumber.Value);
            }

            if (inviteUser is null)
            {
                return BadRequest("No user is found with that information.");
            }

            Result userInviteResult = await _leagueMemberService.InviteUserByUserID(league.Value, inviteUser);
            if (userInviteResult.IsFailure)
            {
                return BadRequest(userInviteResult.Error);
            }

            await _emailSender.SendLeagueInviteEmail(inviteUser.Email, league.Value, baseURL);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateInviteLink([FromBody] CreateInviteLinkRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            IReadOnlyList<LeagueInviteLink> activeLinks = await _leagueMemberService.GetActiveInviteLinks(league.Value);
            if (activeLinks.Count >= 2)
            {
                return BadRequest("You can't have more than 2 invite links active.");
            }

            await _leagueMemberService.CreateInviteLink(league.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteInviteLink([FromBody] DeleteInviteLinkRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var activeLinks = await _leagueMemberService.GetActiveInviteLinks(league.Value);
            var thisLink = activeLinks.SingleOrDefault(x => x.InviteID == request.InviteID);
            if (thisLink is null)
            {
                return BadRequest();
            }

            await _leagueMemberService.DeactivateInviteLink(thisLink);

            return Ok();
        }

        [HttpGet("{leagueID}")]
        public async Task<IActionResult> InviteLinks(Guid leagueID)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(leagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            int currentYear = league.Value.Years.Max();
            string baseURL = $"{Request.Scheme}://{Request.Host.Value}";
            IReadOnlyList<LeagueInviteLink> activeLinks = await _leagueMemberService.GetActiveInviteLinks(league.Value);
            var viewModels = activeLinks.Select(x => new LeagueInviteLinkViewModel(x, currentYear, baseURL));
            return Ok(viewModels);
        }

        [HttpPost]
        public async Task<IActionResult> RescindInvite([FromBody] DeleteInviteRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            Maybe<LeagueInvite> invite = await _leagueMemberService.GetInvite(request.InviteID);
            if (invite.HasNoValue)
            {
                return BadRequest();
            }

            if (invite.Value.League.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            await _leagueMemberService.DeleteInvite(invite.Value);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemovePlayer([FromBody] PlayerRemoveRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            if (league.Value.LeagueManager.Id == request.UserID)
            {
                return BadRequest("Can't remove the league manager.");
            }

            var removeUser = await _userManager.FindByIdAsync(request.UserID.ToString());
            if (removeUser == null)
            {
                return BadRequest();
            }

            var playersInLeague = await _leagueMemberService.GetUsersInLeague(league.Value);
            bool userIsInLeague = playersInLeague.Any(x => x.Id == removeUser.Id);
            if (!userIsInLeague)
            {
                return BadRequest("That user is not in that league.");
            }

            await _leagueMemberService.FullyRemovePlayerFromLeague(league.Value, removeUser);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> EditPublisher([FromBody] PublisherEditRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            if (publisher.Value.LeagueYear.League.LeagueID != league.Value.LeagueID)
            {
                return Forbid();
            }

            var editValues = request.ToDomain(publisher.Value);
            
            Result result = await _publisherService.EditPublisher(editValues);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemovePublisher([FromBody] PublisherRemoveRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            if (publisher.Value.LeagueYear.League.LeagueID != league.Value.LeagueID)
            {
                return Forbid();
            }

            if (publisher.Value.LeagueYear.PlayStatus.PlayStarted)
            {
                return BadRequest();
            }

            await _publisherService.FullyRemovePublisher(publisher.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SetPlayerActiveStatus([FromBody] LeaguePlayerActiveRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(request.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (leagueYear.Value.PlayStatus.PlayStarted)
            {
                return BadRequest("You can't change a player's status for a year that is already started.");
            }

            var publishers = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);

            Dictionary<FantasyCriticUser, bool> userActiveStatus = new Dictionary<FantasyCriticUser, bool>();
            foreach (var userKeyValue in request.ActiveStatus)
            {
                var domainUser = await _userManager.FindByIdAsync(userKeyValue.Key.ToString());
                if (domainUser == null)
                {
                    return BadRequest();
                }

                var publisherForUser = publishers.SingleOrDefault(x => x.User.Id == domainUser.Id);
                if (publisherForUser != null && !userKeyValue.Value)
                {
                    return BadRequest("You must remove a player's publisher before you can set them as inactive.");
                }

                userActiveStatus.Add(domainUser, userKeyValue.Value);
            }

            var result = await _leagueMemberService.SetPlayerActiveStatus(leagueYear.Value, userActiveStatus);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SetAutoDraft([FromBody] ManagerSetAutoDraftRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(request.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            var publishers = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);
            var publisherDictionary = publishers.ToDictionary(x => x.PublisherID);
            foreach (var requestPublisher in request.PublisherAutoDraft)
            {
                if (!publisherDictionary.ContainsKey(requestPublisher.Key))
                {
                    return Forbid();
                }

                await _publisherService.SetAutoDraft(publisherDictionary[requestPublisher.Key], requestPublisher.Value);
            }

            var draftComplete = await _draftService.RunAutoDraftAndCheckIfComplete(leagueYear.Value);
            if (draftComplete)
            {
                await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("DraftFinished");
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ManagerClaimGame([FromBody] ClaimGameRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(publisher.Value.LeagueYear.League.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, publisher.Value.LeagueYear.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (!leagueYear.Value.PlayStatus.DraftFinished)
            {
                return BadRequest("You can't manually manage games until you draft.");
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var claimUser = await _userManager.FindByIdAsync(publisher.Value.User.Id.ToString());
            if (claimUser == null)
            {
                return BadRequest();
            }

            Maybe<MasterGame> masterGame = Maybe<MasterGame>.None;
            if (request.MasterGameID.HasValue)
            {
                masterGame = await _interLeagueService.GetMasterGame(request.MasterGameID.Value);
            }

            ClaimGameDomainRequest domainRequest = new ClaimGameDomainRequest(publisher.Value, request.GameName, request.CounterPick, request.ManagerOverride, false, masterGame, null, null);
            var publishersInLeague = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);
            ClaimResult result = await _gameAcquisitionService.ClaimGame(domainRequest, true, false, publishersInLeague);
            var viewModel = new ManagerClaimResultViewModel(result);

            await _fantasyCriticService.UpdatePublisherGameCalculatedStats(leagueYear.Value);
            return Ok(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ManagerAssociateGame([FromBody] AssociateGameRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(publisher.Value.LeagueYear.League.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, publisher.Value.LeagueYear.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (!leagueYear.Value.PlayStatus.DraftFinished)
            {
                return BadRequest("You cannot manually associate games until you draft.");
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var claimUser = await _userManager.FindByIdAsync(publisher.Value.User.Id.ToString());
            if (claimUser == null)
            {
                return BadRequest();
            }

            var publisherGame = publisher.Value.PublisherGames.SingleOrDefault(x => x.PublisherGameID == request.PublisherGameID);
            if (publisherGame == null)
            {
                return BadRequest();
            }

            Maybe<MasterGame> masterGame = await _interLeagueService.GetMasterGame(request.MasterGameID);
            if (masterGame.HasNoValue)
            {
                return BadRequest();
            }

            AssociateGameDomainRequest domainRequest = new AssociateGameDomainRequest(publisher.Value, publisherGame, masterGame.Value, request.ManagerOverride);

            ClaimResult result = await _gameAcquisitionService.AssociateGame(domainRequest);
            var viewModel = new ManagerClaimResultViewModel(result);

            await _fantasyCriticService.UpdatePublisherGameCalculatedStats(leagueYear.Value);

            return Ok(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RemovePublisherGame([FromBody] GameRemoveRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(publisher.Value.LeagueYear.League.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            Maybe<PublisherGame> publisherGame = await _publisherService.GetPublisherGame(request.PublisherGameID);
            if (publisherGame.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, publisher.Value.LeagueYear.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            Result result = await _publisherService.RemovePublisherGame(publisher.Value, publisherGame.Value);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            await _fantasyCriticService.UpdatePublisherGameCalculatedStats(leagueYear.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ManuallyScorePublisherGame([FromBody] ManualPublisherGameScoreRequest request)
        {
            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            Maybe<LeagueYear> leagueYear = await _fantasyCriticService.GetLeagueYear(publisher.Value.LeagueYear.League.LeagueID, publisher.Value.LeagueYear.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }
            if (!leagueYear.Value.PlayStatus.DraftFinished)
            {
                return BadRequest("You cannot manually score a game until after you draft.");
            }

            return await UpdateManualCriticScore(request.PublisherID, request.PublisherGameID, request.ManualCriticScore);
        }

        [HttpPost]
        public Task<IActionResult> RemoveManualPublisherGameScore([FromBody] ManualPublisherGameScoreRequest request)
        {
            return UpdateManualCriticScore(request.PublisherID, request.PublisherGameID, null);
        }

        private async Task<IActionResult> UpdateManualCriticScore(Guid publisherID, Guid publisherGameID, decimal? manualCriticScore)
        {
            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var publisher = await _publisherService.GetPublisher(publisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(publisher.Value.LeagueYear.League.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            Maybe<PublisherGame> publisherGame = await _publisherService.GetPublisherGame(publisherGameID);
            if (publisherGame.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, publisher.Value.LeagueYear.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            await _fantasyCriticService.ManuallyScoreGame(publisherGame.Value, manualCriticScore);
            await _fantasyCriticService.UpdatePublisherGameCalculatedStats(leagueYear.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ManuallySetWillNotRelease([FromBody] ManualPublisherGameWillNotReleaseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(publisher.Value.LeagueYear.League.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            Maybe<LeagueYear> leagueYear = await _fantasyCriticService.GetLeagueYear(publisher.Value.LeagueYear.League.LeagueID, publisher.Value.LeagueYear.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }
            if (!leagueYear.Value.PlayStatus.DraftFinished)
            {
                return BadRequest("You cannot set a game as will not release until after you draft.");
            }

            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            Maybe<PublisherGame> publisherGame = await _publisherService.GetPublisherGame(request.PublisherGameID);
            if (publisherGame.HasNoValue)
            {
                return BadRequest();
            }

            await _fantasyCriticService.ManuallySetWillNotRelease(leagueYear.Value, publisherGame.Value, request.WillNotRelease);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> StartDraft([FromBody] StartDraftRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (!leagueYear.Value.PlayStatus.Equals(PlayStatus.NotStartedDraft))
            {
                return BadRequest();
            }

            var supportedYear = (await _interLeagueService.GetSupportedYears()).SingleOrDefault(x => x.Year == request.Year);
            if (supportedYear is null)
            {
                return BadRequest();
            }

            var activeUsers = await _leagueMemberService.GetActivePlayersForLeagueYear(league.Value, request.Year);

            var publishersInLeague = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);
            bool readyToPlay = _draftService.LeagueIsReadyToPlay(supportedYear, publishersInLeague, activeUsers);
            if (!readyToPlay)
            {
                return BadRequest();
            }

            var draftComplete = await _draftService.StartDraft(leagueYear.Value);
            await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("RefreshLeagueYear");

            if (draftComplete)
            {
                await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("DraftFinished");
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ResetDraft([FromBody] ResetDraftRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (leagueYear.Value.PlayStatus.Equals(PlayStatus.NotStartedDraft) || leagueYear.Value.PlayStatus.Equals(PlayStatus.DraftFinal))
            {
                return BadRequest();
            }

            var supportedYear = (await _interLeagueService.GetSupportedYears()).SingleOrDefault(x => x.Year == request.Year);
            if (supportedYear is null)
            {
                return BadRequest();
            }

            await _draftService.ResetDraft(leagueYear.Value);
            await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("RefreshLeagueYear");

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SetDraftOrder([FromBody] DraftOrderRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (leagueYear.Value.PlayStatus.PlayStarted)
            {
                return BadRequest();
            }

            var activeUsers = await _leagueMemberService.GetActivePlayersForLeagueYear(league.Value, request.Year);
            var publishersInLeague = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);
            var readyToSetDraftOrder = _draftService.LeagueIsReadyToSetDraftOrder(publishersInLeague, activeUsers);
            if (!readyToSetDraftOrder)
            {
                return BadRequest();
            }

            List<KeyValuePair<Publisher, int>> draftPositions = new List<KeyValuePair<Publisher, int>>();
            for (var index = 0; index < request.PublisherDraftPositions.Count; index++)
            {
                var requestPublisher = request.PublisherDraftPositions[index];
                var publisher = publishersInLeague.SingleOrDefault(x => x.PublisherID == requestPublisher);
                if (publisher is null)
                {
                    return BadRequest();
                }

                draftPositions.Add(new KeyValuePair<Publisher, int>(publisher, index + 1));
            }

            var result = await _draftService.SetDraftOrder(leagueYear.Value, draftPositions);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ManagerDraftGame([FromBody] ManagerDraftGameRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var publisher = await _publisherService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(publisher.Value.LeagueYear.League.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, publisher.Value.LeagueYear.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (!leagueYear.Value.PlayStatus.DraftIsActive)
            {
                return BadRequest("You can't draft a game if the draft isn't active.");
            }

            var publishersInLeague = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);
            var nextPublisher = _draftService.GetNextDraftPublisher(leagueYear.Value, publishersInLeague);
            if (nextPublisher.HasNoValue)
            {
                return BadRequest("There are no spots open to draft.");
            }

            if (!nextPublisher.Value.Equals(publisher.Value))
            {
                return BadRequest("That publisher is not next up for drafting.");
            }

            Maybe<MasterGame> masterGame = Maybe<MasterGame>.None;
            if (request.MasterGameID.HasValue)
            {
                masterGame = await _interLeagueService.GetMasterGame(request.MasterGameID.Value);
            }

            var draftPhase = _draftService.GetDraftPhase(leagueYear.Value, publishersInLeague);
            if (draftPhase.Equals(DraftPhase.StandardGames))
            {
                if (request.CounterPick)
                {
                    return BadRequest("Not drafting counterPicks now.");
                }
            }

            if (draftPhase.Equals(DraftPhase.CounterPicks))
            {
                if (!request.CounterPick)
                {
                    return BadRequest("Not drafting standard games now.");
                }
            }

            var draftStatus = _draftService.GetDraftStatus(leagueYear.Value, publishersInLeague);
            ClaimGameDomainRequest domainRequest = new ClaimGameDomainRequest(publisher.Value, request.GameName, request.CounterPick, request.ManagerOverride, false,
                masterGame, draftStatus.DraftPosition, draftStatus.OverallDraftPosition);

            var result = await _draftService.DraftGame(domainRequest, true, leagueYear.Value, publishersInLeague);
            var viewModel = new ManagerClaimResultViewModel(result.Result);
            await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("RefreshLeagueYear");

            if (result.DraftComplete)
            {
                await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("DraftFinished");
            }

            return Ok(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SetDraftPause([FromBody] DraftPauseRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (request.Pause)
            {
                if (!leagueYear.Value.PlayStatus.Equals(PlayStatus.Drafting))
                {
                    return BadRequest();
                }
            }
            if (!request.Pause)
            {
                if (!leagueYear.Value.PlayStatus.Equals(PlayStatus.DraftPaused))
                {
                    return BadRequest();
                }
            }

            await _draftService.SetDraftPause(leagueYear.Value, request.Pause);
            await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("RefreshLeagueYear");

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UndoLastDraftAction([FromBody] UndoLastDraftActionRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (!leagueYear.Value.PlayStatus.Equals(PlayStatus.DraftPaused))
            {
                return BadRequest("Can only undo when the draft is paused.");
            }

            var publishers = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);
            bool hasGames = publishers.SelectMany(x => x.PublisherGames).Any();
            if (!hasGames)
            {
                return BadRequest("Can't undo a drafted game if no games have been drafted.");
            }

            await _draftService.UndoLastDraftAction(publishers);
            await _hubContext.Clients.Group(leagueYear.Value.GetGroupName).SendAsync("RefreshLeagueYear");

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SetGameEligibilityOverride([FromBody] EligiblityOverrideRequest request)
        {
            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            Maybe<MasterGame> masterGame = await _interLeagueService.GetMasterGame(request.MasterGameID);
            if (masterGame.HasNoValue)
            {
                return BadRequest();
            }

            if (masterGame.Value.ReleaseDate.HasValue && masterGame.Value.ReleaseDate.Value.Year < leagueYear.Value.Year)
            {
                return BadRequest("You can't change the override setting of a game that came out in a previous year.");
            }

            var currentDate = _clock.GetToday();
            var eligibilityFactors = leagueYear.Value.GetEligibilityFactorsForMasterGame(masterGame.Value, currentDate);
            bool alreadyEligible = SlotEligibilityService.GameIsEligibleInLeagueYear(eligibilityFactors);
            bool isAllowing = request.Eligible.HasValue && request.Eligible.Value;
            bool isBanning = request.Eligible.HasValue && !request.Eligible.Value;

            if (isAllowing && alreadyEligible)
            {
                return BadRequest("That game is already eligible in your league.");
            }

            if (isBanning && !alreadyEligible)
            {
                return BadRequest("That game is already ineligible in your league.");
            }

            if (!isAllowing)
            {
                var publishers = await _publisherService.GetPublishersInLeagueForYear(leagueYear.Value);
                var matchingPublisherGame = publishers
                    .SelectMany(x => x.PublisherGames)
                    .FirstOrDefault(x =>
                        x.MasterGame.HasValue &&
                        x.MasterGame.Value.MasterGame.MasterGameID == masterGame.Value.MasterGameID);
                if (matchingPublisherGame != null)
                {
                    return BadRequest("You can't change the override setting of a game that someone in your league has.");
                }
            }

            await _fantasyCriticService.SetEligibilityOverride(leagueYear.Value, masterGame.Value, request.Eligible);
            var refreshedLeagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (refreshedLeagueYear.HasNoValue)
            {
                return BadRequest();
            }
            await _fantasyCriticService.UpdatePublisherGameCalculatedStats(refreshedLeagueYear.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SetGameTagOverride([FromBody] TagOverrideRequest request)
        {
            var systemWideSettings = await _interLeagueService.GetSystemWideSettings();
            if (systemWideSettings.ActionProcessingMode)
            {
                return BadRequest();
            }

            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            Maybe<MasterGame> masterGame = await _interLeagueService.GetMasterGame(request.MasterGameID);
            if (masterGame.HasNoValue)
            {
                return BadRequest();
            }

            if (masterGame.Value.ReleaseDate.HasValue && masterGame.Value.ReleaseDate.Value.Year < leagueYear.Value.Year)
            {
                return BadRequest("You can't override the tags of a game that came out in a previous year.");
            }

            IReadOnlyList<MasterGameTag> currentOverrideTags = await _fantasyCriticService.GetTagOverridesForGame(league.Value, leagueYear.Value.Year, masterGame.Value);

            var allTags = await _interLeagueService.GetMasterGameTags();
            var requestedTags = allTags.Where(x => request.Tags.Contains(x.Name)).ToList();
            if (ListExtensions.SequencesContainSameElements(masterGame.Value.Tags, requestedTags))
            {
                return BadRequest("That game already has those exact tags.");
            }

            if (ListExtensions.SequencesContainSameElements(currentOverrideTags, requestedTags))
            {
                return BadRequest("That game is already overriden to have those exact tags.");
            }

            await _fantasyCriticService.SetTagOverride(leagueYear.Value, masterGame.Value, requestedTags);
            var refreshedLeagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (refreshedLeagueYear.HasNoValue)
            {
                return BadRequest();
            }
            await _fantasyCriticService.UpdatePublisherGameCalculatedStats(refreshedLeagueYear.Value);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> PromoteNewLeagueManager([FromBody] PromoteNewLeagueManagerRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            var newManager = await _userManager.FindByIdAsync(request.NewManagerUserID.ToString());
            var usersInLeague = await _leagueMemberService.GetUsersInLeague(league.Value);
            if (!usersInLeague.Contains(newManager))
            {
                return BadRequest();
            }

            await _leagueMemberService.TransferLeagueManager(league.Value, newManager);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> PostNewManagerMessage([FromBody] PostNewManagerMessageRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            await _fantasyCriticService.PostNewManagerMessage(leagueYear.Value, request.Message, request.IsPublic);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteManagerMessage([FromBody] DeleteManagerMessageRequest request)
        {
            var currentUserResult = await GetCurrentUser();
            if (currentUserResult.IsFailure)
            {
                return BadRequest(currentUserResult.Error);
            }
            var currentUser = currentUserResult.Value;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, request.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (league.Value.LeagueManager.Id != currentUser.Id)
            {
                return Forbid();
            }

            await _fantasyCriticService.DeleteManagerMessage(request.MessageID);

            return Ok();
        }
    }
}
