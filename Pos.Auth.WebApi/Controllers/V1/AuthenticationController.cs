using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Features.Authentication.Commands.ConfirmEmail;
using Pos.Identity.Application.Features.Authentication.Commands.DeactivateUser;
using Pos.Identity.Application.Features.Authentication.Commands.ForgotPassword;
using Pos.Identity.Application.Features.Authentication.Commands.Login;
using Pos.Identity.Application.Features.Authentication.Commands.RegisterCommand;
using Pos.Identity.Application.Features.Authentication.Commands.ResetPassword;
using Pos.Identity.Application.Features.Authentication.Commands.SocialLogin;
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


        [HttpPost("~/connect/token")]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var oidcRequest = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("OpenIddict request cannot be retrieved.");

            if (oidcRequest.IsPasswordGrantType())
            {
                Response<LoginResult> loginResult;
                try
                {
                    loginResult = await _mediator.Send(new LoginCommand
                    {
                        Email = oidcRequest.Username,
                        Password = oidcRequest.Password
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

                return SignIn(BuildPrincipal(loginResult.Data), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (oidcRequest.IsRefreshTokenGrantType())
            {

                var result = await HttpContext.AuthenticateAsync(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                var userId = result.Principal.GetClaim(Claims.Subject);
                var loginResult = await _mediator.Send(new GetUserStatusQuery { UserId = userId });

                if (!loginResult.Data.IsActive)
                {
                    return Forbid(
                        new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error]
                                = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription]
                                = "This account has been deactivated."
                        }),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return Unauthorized();
        }

        // ── Social Login ──────────────────────────────────
        [HttpGet("~/connect/authorize")]
        public IActionResult Authorize(string provider)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), new { provider })
            };
            return Challenge(properties, provider);
        }


        [HttpGet("~/connect/callback")]
        public async Task<IActionResult> Callback(string provider)
        {
            var externalResult = await HttpContext.AuthenticateAsync(provider);

            if (!externalResult.Succeeded)
                return Forbid();

            var externalClaims = externalResult.Principal.Claims;

            var providerKey = externalClaims
               .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? throw new InvalidOperationException("No provider key found.");

            var email = externalClaims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value
                ?? throw new InvalidOperationException("No email found.");

            var fullName = externalClaims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value
                ?? email;
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

            return SignIn(BuildPrincipal(loginResult.Data),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }


        private static ClaimsPrincipal BuildPrincipal(LoginResult data)
        {
            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.AddClaim(Claims.Subject, data.UserId, Destinations.AccessToken);
            identity.AddClaim(Claims.Email, data.Email, Destinations.AccessToken);
            identity.AddClaim(Claims.Name, data.Email, Destinations.AccessToken);
            identity.AddClaim(Claims.GivenName, data.FullName, Destinations.AccessToken);

            foreach (var role in data.Roles)
                identity.AddClaim(Claims.Role, role, Destinations.AccessToken);

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(
                Scopes.OpenId,
                Scopes.Email,
                Scopes.Profile,
                Scopes.Roles,
                Scopes.OfflineAccess,
                "api");

            return principal;
        }
    }
}
