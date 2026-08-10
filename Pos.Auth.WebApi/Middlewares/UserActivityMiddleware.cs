using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Pos.Identity.Domain.Models;
using System.Security.Claims;

namespace Pos.Auth.WebApi.Middlewares
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserActivityMiddleware> _logger;

        public UserActivityMiddleware(
            RequestDelegate next,
            ILogger<UserActivityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUser> userManager)
        {
            var hasBearerToken = context.Request.Headers.Authorization
                .ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

            if (!hasBearerToken)
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirstValue(OpenIddictConstants.Claims.Subject)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                await _next(context);
                return;
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                await _next(context);
                return;
            }

            var now = DateTime.UtcNow;

            user.LastAccessedAt = now;
            user.UpdatedAt = now;

            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                _logger.LogWarning(
                    "Failed to update LastAccessedAt for UserId {UserId}",
                    user.Id);
            }

            await _next(context);
        }
    }
}
