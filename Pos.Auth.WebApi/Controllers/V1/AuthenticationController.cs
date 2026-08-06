using Asp.Versioning;
using Azure.Core;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Pos.Identity.Application.Common.Security;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Features.Authentication.Commands.ConfirmEmail;
using Pos.Identity.Application.Features.Authentication.Commands.DeactivateUser;
using Pos.Identity.Application.Features.Authentication.Commands.ForgotPassword;
using Pos.Identity.Application.Features.Authentication.Commands.Login;
using Pos.Identity.Application.Features.Authentication.Commands.RegisterCommand;
using Pos.Identity.Application.Features.Authentication.Commands.ResetPassword;
using Pos.Identity.Application.Features.Authentication.Commands.SocialLogin;
using Pos.Identity.Application.Features.Authentication.DTOS;
using Pos.Identity.Application.Features.Authentication.Querys.GetById;
using Pos.Identity.Application.Features.Authentication.Querys.GetUserStatus;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Constants;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Pos.Identity.WebApi.Controllers.V1
{
    [Route("api/{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AuthenticationController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost("Register")]
        public async Task<ActionResult<Response<string>>> Register([FromBody] RegisterUserCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("ConfirmEmail")]
        public async Task<ActionResult<Response<string>>> ConfirmEmail([FromQuery] ConfirmEmailCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<Response<string>>> ForgotPassword([FromBody] ForgotPasswordCommand command)
        => Ok(await _mediator.Send(command));

        [HttpPost("reset-password")]
        public async Task<ActionResult<Response<string>>> ResetPassword([FromQuery] ResetPasswordCommand command)
            => Ok(await _mediator.Send(command));

        [HttpPost("deactivate/{userId}")]
        [Authorize(Roles = PlatformRoles.Admin)]
        public async Task<ActionResult<Response<string>>> Deactivate(string userId)
            => Ok(await _mediator.Send(new DeactivateUserCommand { UserId = userId }));

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            if (!Url.IsLocalUrl(command.ReturnUrl))
                return BadRequest(new { error = "Invalid return URL." });
            Response<LoginResult> loginResult;
            try
            {
                loginResult = await _mediator.Send(new LoginCommand
                {
                    Email = command.Email,
                    Password = command.Password
                });
            }
            catch (ApiException ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            var cookieIdentity = new ClaimsIdentity(
                AuthenticationSchemes.ApplicationCookie);

            cookieIdentity.AddClaim(new Claim(
                ClaimTypes.NameIdentifier,
                loginResult.Data.UserId));

            await HttpContext.SignInAsync(
                AuthenticationSchemes.ApplicationCookie,
                new ClaimsPrincipal(cookieIdentity));

            return Ok(new { returnUrl = command.ReturnUrl, message = "Login successful." });
        }

        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("OpenIddict request cannot be retrieved.");

            // Check if user already has a session
            var authentication = await HttpContext.AuthenticateAsync(
                AuthenticationSchemes.ApplicationCookie);

            var userId = authentication.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            // No session — redirect to login, come back here after
            if (!authentication.Succeeded || string.IsNullOrWhiteSpace(userId))
            {
                return Challenge(
                    new AuthenticationProperties
                    {
                        RedirectUri = Request.PathBase + Request.Path + Request.QueryString
                    },
                    AuthenticationSchemes.ApplicationCookie);
            }

            // Session exists — build minimal identity for the authorization code
            var identity = new ClaimsIdentity(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                Claims.Name,
                Claims.Role);

            identity.AddClaim(Claims.Subject, userId);

            var principal = new ClaimsPrincipal(identity);

            SetGrantedScopes(principal, request);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/token")]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var oidcRequest = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("OpenIddict request cannot be retrieved.");

            // ── Authorization Code Grant  ───────────
            if (oidcRequest.IsAuthorizationCodeGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                if (!result.Succeeded || result.Principal is null)
                    return InvalidGrant("The authorization code is invalid.");

                var userId = result.Principal.GetClaim(Claims.Subject);

                if (string.IsNullOrWhiteSpace(userId))
                    return InvalidGrant("The authorization code has no user.");

                // Load full user from DB — auth code only carried userId
                var userResult = await _mediator.Send(
                    new GetUserForTokenQuery(userId));

                if (!userResult.Data.IsActive)
                    return InvalidGrant("This account has been deactivated.");

                var tokenPrincipal = BuildPrincipalCore(userResult.Data);

                tokenPrincipal.SetScopes(result.Principal.GetScopes());

                return SignIn(result.Principal,
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // ── Refresh Token Grant ───────────────────────────────
            if (oidcRequest.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                var userId = result.Principal.GetClaim(Claims.Subject);
                var statusResult = await _mediator.Send(
                    new GetUserStatusQuery { UserId = userId });

                if (!statusResult.Data.IsActive)
                    return Forbid(
                        new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error]
                                = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription]
                                = "This account has been deactivated."
                        }),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                return SignIn(result.Principal,
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return Unauthorized();
        }

        // ── Social Login ──────────────────────────────────


        [HttpGet("~/connect/external-login")]
        public IActionResult ExternalLogin(
           [FromQuery] string provider,
           [FromQuery] string returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
                return BadRequest(new { error = "Invalid return URL." });

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), new { returnUrl })
            };

            return Challenge(properties, provider);
        }


        [HttpGet("~/connect/callback")]
        public async Task<IActionResult> Callback([FromQuery] string returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
                return BadRequest("The return URL is invalid.");

            var externalResult = await HttpContext.AuthenticateAsync(
                IdentityConstants.ExternalScheme);

            if (!externalResult.Succeeded)
                return Forbid();

            // ✅ Provider name read from the result itself — cannot be spoofed
            var provider = externalResult.Properties?.Items[".AuthScheme"]
                ?? throw new InvalidOperationException("No provider found in external result.");

            if (string.IsNullOrWhiteSpace(provider))
                return BadRequest("The external provider is missing.");

            var providerKey = externalResult.Principal
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var email = externalResult.Principal
                .FindFirstValue(ClaimTypes.Email);

            var fullName = externalResult.Principal
                .FindFirstValue(ClaimTypes.Name) ?? email;

            if (string.IsNullOrWhiteSpace(providerKey) ||
                string.IsNullOrWhiteSpace(email))
                return BadRequest("Required external identity claims are missing.");

            Response<LoginResult> loginResult;
            try
            {
                loginResult = await _mediator.Send(new SocialLoginCommand
                {
                    Provider = provider,
                    ProviderKey = providerKey,
                    Email = email,
                    FullName = fullName
                });
            }
            catch (ApiException ex)
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error]
                            = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription]
                            = ex.Message
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Minimal cookie — mirrors exactly what /login does
            var cookieIdentity = new ClaimsIdentity(
                AuthenticationSchemes.ApplicationCookie);

            cookieIdentity.AddClaim(new Claim(
                ClaimTypes.NameIdentifier,
                loginResult.Data.UserId));

            await HttpContext.SignInAsync(
                AuthenticationSchemes.ApplicationCookie,
                new ClaimsPrincipal(cookieIdentity));

            // Redirect to /connect/authorize — same next step as password flow
            return LocalRedirect(returnUrl);
        }


        //Helpers

        private IActionResult InvalidGrant(string description)
        {
            return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error]
                            = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription]
                            = description
                    }),OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
            
        private static void SetGrantedScopes(ClaimsPrincipal principal,OpenIddictRequest request)
        {
            var supportedScopes = new HashSet<string>(StringComparer.Ordinal)
            {
                Scopes.OpenId,
                Scopes.Profile,
                Scopes.Email,
                Scopes.Roles,
                Scopes.OfflineAccess
            };

            principal.SetScopes(request.GetScopes()
                .Where(supportedScopes.Contains));
        }

        private static ClaimsPrincipal BuildPrincipalCore(UserForTokenResult data)
        {
            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.AddClaim(new Claim(Claims.Subject, data.UserId)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

            identity.AddClaim(new Claim(Claims.Email, data.Email)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

            identity.AddClaim(new Claim(Claims.Name, data.Email)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

            identity.AddClaim(new Claim(Claims.GivenName, data.FullName)
                .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

            identity.AddClaim(new Claim(CustomClaimTypes.UserType, data.UserType)
                .SetDestinations(Destinations.AccessToken));

            if (data.TenantId.HasValue)
                identity.AddClaim(new Claim(
                    CustomClaimTypes.TenantId,
                    data.TenantId.Value.ToString())
                    .SetDestinations(Destinations.AccessToken));

            foreach (var role in data.Roles)
                identity.AddClaim(new Claim(Claims.Role, role)
                    .SetDestinations(Destinations.AccessToken));

            return new ClaimsPrincipal(identity);
        }
    }
}
