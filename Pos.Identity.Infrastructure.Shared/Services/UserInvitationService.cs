using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Domain.Constants;
using Pos.Identity.Domain.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Services
{
    public class UserInvitationService : IUserInvitationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserInvitationService> _logger;

        public UserInvitationService(
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            ILogger<UserInvitationService> logger)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task ResendInvitationAsync(string userId,CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new ApiException("User is not authenticated.");

            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser == null)
                throw new ApiException("Current user not found.");

            if (!currentUser.IsActive)
                throw new ApiException("This account has been deactivated.");

            var targetUser = await _userManager.FindByIdAsync(userId);

            if (targetUser == null)
                throw new ApiException("User not found.");

            ValidateTargetUserForResend(targetUser);

            await EnsureCanResendInvitationAsync(currentUser, targetUser);

            var temporaryPassword = GenerateTemporaryPassword();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var hasPassword = await _userManager.HasPasswordAsync(targetUser);

            if (hasPassword)
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(targetUser);

                if (!removePasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));

                    _logger.LogError(
                        "Failed to remove old temporary password. TargetUserId: {TargetUserId}. Errors: {Errors}",
                        targetUser.Id,
                        errors);

                    throw new ApiException($"Failed to remove old temporary password: {errors}");
                }
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(
                targetUser,
                temporaryPassword);

            if (!addPasswordResult.Succeeded)
            {
                var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));

                _logger.LogError(
                    "Failed to set new temporary password. TargetUserId: {TargetUserId}. Errors: {Errors}",
                    targetUser.Id,
                    errors);

                throw new ApiException($"Failed to set new temporary password: {errors}");
            }

            targetUser.MustChangePassword = true;
            targetUser.TemporaryPasswordExpiresAt = expiresAt;
            targetUser.EmailConfirmed = true;
            targetUser.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(targetUser);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));

                _logger.LogError(
                    "Failed to update invitation state. TargetUserId: {TargetUserId}. Errors: {Errors}",
                    targetUser.Id,
                    errors);

                throw new ApiException($"Failed to update invitation state: {errors}");
            }

            var portalName = GetPortalName(targetUser);

            await _emailService.SendUserInvitationEmailAsync(
                targetUser.Email!,
                targetUser.FullName,
                temporaryPassword,
                expiresAt,
                portalName,
                cancellationToken);

            _logger.LogInformation(
                "Invitation resent successfully. TargetUserId: {TargetUserId}, Email: {Email}, ByUserId: {CurrentUserId}",
                targetUser.Id,
                targetUser.Email,
                currentUser.Id);
        }
        private static void ValidateTargetUserForResend(ApplicationUser targetUser)
        {
            if (!targetUser.IsActive)
                throw new ApiException("Target user account is deactivated.");

            if (string.IsNullOrWhiteSpace(targetUser.Email))
                throw new ApiException("Target user email is missing.");

            if (string.IsNullOrWhiteSpace(targetUser.FullName))
                throw new ApiException("Target user full name is missing.");

            if (!targetUser.MustChangePassword)
                throw new ApiException("This user has already changed their password.");
        }

        private async Task EnsureCanResendInvitationAsync(
            ApplicationUser currentUser,
            ApplicationUser targetUser)
        {
            if (currentUser.UserType == UserTypes.Platform)
            {
                await EnsurePlatformUserCanResendAsync(currentUser, targetUser);
                return;
            }

            if (currentUser.UserType == UserTypes.Tenant)
            {
                await EnsureTenantUserCanResendAsync(currentUser, targetUser);
                return;
            }

            throw new ApiException("Invalid current user type.");
        }

        private async Task EnsurePlatformUserCanResendAsync(
            ApplicationUser currentUser,
            ApplicationUser targetUser)
        {
            var isSuperAdmin = await _userManager.IsInRoleAsync(
                currentUser,
                PlatformRoles.SuperAdmin);

            var isPlatformAdmin = await _userManager.IsInRoleAsync(
                currentUser,
                PlatformRoles.Admin);

            if (!isSuperAdmin && !isPlatformAdmin)
                throw new ApiException("You do not have permission to resend this invitation.");

            if (targetUser.UserType != UserTypes.Platform)
                throw new ApiException("Platform users can only resend platform admin invitations.");

            if (targetUser.TenantId.HasValue)
                throw new ApiException("Invalid platform user state.");

            var targetIsSuperAdmin = await _userManager.IsInRoleAsync(
                targetUser,
                PlatformRoles.SuperAdmin);

            if (!isSuperAdmin && targetIsSuperAdmin)
                throw new ApiException("Only SuperAdmin can resend invitation to another SuperAdmin.");
        }

        private async Task EnsureTenantUserCanResendAsync(
            ApplicationUser currentUser,
            ApplicationUser targetUser)
        {
            if (!currentUser.TenantId.HasValue)
                throw new ApiException("Current user is not linked to a tenant.");

            var isTenantOwner = await _userManager.IsInRoleAsync(
                currentUser,
                TenantRoles.TenantOwner);

            var isTenantAdmin = await _userManager.IsInRoleAsync(
                currentUser,
                TenantRoles.Admin);

            if (!isTenantOwner && !isTenantAdmin)
                throw new ApiException("You do not have permission to resend this invitation.");

            if (targetUser.UserType != UserTypes.Tenant)
                throw new ApiException("Tenant users can only resend tenant user invitations.");

            if (!targetUser.TenantId.HasValue ||
                targetUser.TenantId.Value != currentUser.TenantId.Value)
            {
                throw new ApiException("You cannot manage users outside your tenant.");
            }

            var targetIsTenantOwner = await _userManager.IsInRoleAsync(
                targetUser,
                TenantRoles.TenantOwner);

            var targetIsTenantAdmin = await _userManager.IsInRoleAsync(
                targetUser,
                TenantRoles.Admin);

            if (targetIsTenantOwner)
                throw new ApiException("TenantOwner invitation cannot be resent from this endpoint.");

            if (!isTenantOwner && targetIsTenantAdmin)
                throw new ApiException("Only TenantOwner can resend invitation to tenant admins.");
        }

        private static string GetPortalName(ApplicationUser targetUser)
        {
            return targetUser.UserType == UserTypes.Platform
                ? "Vendora Platform Admin Portal"
                : "Vendora POS";
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@$?_-";
            const string all = upper + lower + digits + special;

           
            var chars = new List<char>
            {
                upper  [RandomNumberGenerator.GetInt32(upper.Length)],
                lower  [RandomNumberGenerator.GetInt32(lower.Length)],
                digits [RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)]
            };

            while (chars.Count < 12)
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            
            return new string(chars
                .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                .ToArray());
        }
    }
}
