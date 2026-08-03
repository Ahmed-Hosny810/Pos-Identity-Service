using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Constants;
using Pos.Identity.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Services
{
    public class PlatformService : IPlatformService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PlatformService> _logger;
        public PlatformService(UserManager<ApplicationUser> userManager,ILogger<PlatformService> logger)
        {
            _userManager = userManager;
            _logger = logger; 
        }
        public async Task<Response<string>> AssignPlatformAdminRoleAsync(string userId)
        {
            return await AssignRoleAsync(userId, PlatformRoles.Admin);
        }

        public async Task<Response<string>> AssignPlatformSuperAdminRoleAsync(string userId)
        {
            return await AssignRoleAsync(userId, PlatformRoles.SuperAdmin);
        }

        private async Task<Response<string>> AssignRoleAsync(string userId, string toRole)
        {
            _logger.LogInformation(
            "Role assignment requested. UserId: {UserId} Role: {Role}",
            userId, toRole);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ApiException("User not found");

            if (await _userManager.IsInRoleAsync(user, toRole))
            {
                _logger.LogWarning(
                "Role already assigned. UserId: {UserId} Role: {Role}",
                userId, toRole);

                throw new ApiException($"User is already assigned the {toRole} role.");
            }

            var result = await _userManager.AddToRoleAsync(user, toRole);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(
                "Failed to assign role for UserId: {UserId} Errors: {Errors}", userId, errors);
                throw new ApiException($"Failed to assign {toRole} role: {errors}");
            }
            _logger.LogInformation(
            "Role assigned successfully. UserId: {UserId} Role: {Role}",
            userId, toRole);

            return new Response<string>(data:$"User successfully assigned the {toRole} role.");


        }
    }
}
