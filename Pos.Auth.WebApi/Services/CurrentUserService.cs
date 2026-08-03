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
    }
}
