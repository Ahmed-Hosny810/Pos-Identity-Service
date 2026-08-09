using OpenIddict.Abstractions;
using Pos.Identity.Application.Interfaces.Services;
using System.Security.Claims;

namespace Pos.Auth.WebApi.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;

                if (principal?.Identity?.IsAuthenticated != true)
                    return null;

                return principal.FindFirstValue(OpenIddictConstants.Claims.Subject)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        public string? AccessToken
        {
            get
            {
                var authorizationHeader =
                    _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

                if (string.IsNullOrWhiteSpace(authorizationHeader))
                    return null;

                const string bearerPrefix = "Bearer ";

                if (!authorizationHeader.StartsWith(
                        bearerPrefix,
                        StringComparison.OrdinalIgnoreCase))
                    return null;

                return authorizationHeader[bearerPrefix.Length..].Trim();
            }
        }
    }
}
