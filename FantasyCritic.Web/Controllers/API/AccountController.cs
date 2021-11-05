using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Cache;
using System.Security.Claims;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain;
using FantasyCritic.Lib.Identity;
using FantasyCritic.Lib.Interfaces;
using FantasyCritic.Lib.Services;
using FantasyCritic.Web.Extensions;
using FantasyCritic.Web.Models;
using FantasyCritic.Web.Models.Requests;
using FantasyCritic.Web.Models.Requests.Account;
using FantasyCritic.Web.Models.Responses;
using FantasyCritic.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace FantasyCritic.Web.Controllers.API
{
    [Route("api/[controller]/[action]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AccountController : ControllerBase
    {
        private readonly FantasyCriticUserManager _userManager;
        private readonly FantasyCriticRoleManager _roleManager;
        private readonly SignInManager<FantasyCriticUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly ITokenService _tokenService;
        private readonly IClock _clock;

        public AccountController(FantasyCriticUserManager userManager, FantasyCriticRoleManager roleManager, SignInManager<FantasyCriticUser> signInManager, 
            IEmailSender emailSender, ILogger<AccountController> logger, ITokenService tokenService, IClock clock)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _tokenService = tokenService;
            _clock = clock;
        }

        public async Task<ActionResult> CurrentUser()
        {
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var roles = await _userManager.GetRolesAsync(currentUser);

            FantasyCriticUserViewModel vm = new FantasyCriticUserViewModel(currentUser, roles);
            return Ok(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest();
            }

            int openUserNumber = await _userManager.GetOpenDisplayNumber(model.DisplayName);

            var user = new FantasyCriticUser(Guid.NewGuid(), model.DisplayName, openUserNumber, model.EmailAddress, model.EmailAddress, false, "", "", _clock.GetCurrentInstant(), false);
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("User created a new account with password.");

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string baseURL = $"{Request.Scheme}://{Request.Host.Value}";
            await _emailSender.SendConfirmationEmail(user, code, baseURL);

            return Created("", user.UserID.ToString());
        }

        [HttpPost]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string baseURL = $"{Request.Scheme}://{Request.Host.Value}";
            await _emailSender.SendConfirmationEmail(user, code, baseURL);

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByEmailAsync(request.EmailAddress);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return Ok();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            string baseURL = $"{Request.Scheme}://{Request.Host.Value}";
            await _emailSender.SendForgotPasswordEmail(user, code, baseURL);

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendChangeEmail([FromBody] SendChangeEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return BadRequest();
            }

            var code = await _userManager.GenerateChangeEmailTokenAsync(user, request.NewEmailAddress);
            string baseURL = $"{Request.Scheme}://{Request.Host.Value}";
            await _emailSender.SendChangeEmail(user, request.NewEmailAddress, code, baseURL);

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByEmailAsync(request.EmailAddress);
            if (user == null)
            {
                return Ok();
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Code, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return BadRequest();
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return Ok();
            }

            var result = await _userManager.ChangeEmailAsync(user, request.NewEmailAddress, request.Code);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string baseURL = $"{Request.Scheme}://{Request.Host.Value}";
            await _emailSender.SendConfirmationEmail(user, code, baseURL);

            return await GetToken(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            await Task.Delay(2 * 1000);
            var user = await _userManager.FindByIdAsync(request.UserID);
            if (user == null)
            {
                return BadRequest();
            }

            var result = await _userManager.ConfirmEmailAsync(user, request.Code);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.EmailAddress);
            if (user == null)
            {
                return BadRequest();
            }

            if (user.IsDeleted)
            {
                return BadRequest();
            }

            var result = await _signInManager.PasswordSignInAsync(user.NormalizedEmail, model.Password, false, false);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            return await GetToken(user);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeDisplayName([FromBody] ChangeDisplayNameRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return BadRequest();
            }

            user.UserName = request.NewDisplayName;
            user.DisplayNumber = await _userManager.GetOpenDisplayNumber(user.DisplayName);
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return BadRequest();
            }

            await _userManager.DeleteUserAccount(user);

            return Ok();
        }

        private async Task<ObjectResult> GetToken(FantasyCriticUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = user.GetUserClaims(roles);
            var jwtToken = _tokenService.GenerateAccessToken(claims);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _userManager.AddRefreshToken(user, refreshToken);
            await _userManager.ClearOldRefreshTokens(user);
            var jwtString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return new ObjectResult(new
            {
                token = jwtString,
                refreshToken,
                expiration = jwtToken.ValidTo
            });
        }
    }
}
