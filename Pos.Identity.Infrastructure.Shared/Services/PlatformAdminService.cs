using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Features.PlatformAdmins.Commands;
using Pos.Identity.Application.Features.PlatformAdmins.DTOS;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Constants;
using Pos.Identity.Domain.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Services
{
    public class PlatformAdminService : IPlatformAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<PlatformAdminService> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;

        public PlatformAdminService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<PlatformAdminService> logger,
            ICurrentUserService currentUserService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _currentUserService = currentUserService;
            _emailService = emailService;
        }

        public async Task<CreatePlatformAdminResult> CreatePlatformAdminAsync(
            CreatePlatformAdminCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            var validation = await ValidateCurrentPlatformUserAsync(
                currentUserId,
                request.Role);

            var currentUser = validation.User;

            var allowedRoles = new[]
            {
             PlatformRoles.SuperAdmin,
             PlatformRoles.Admin
            };

            if (!allowedRoles.Contains(request.Role))
                throw new ApiException("Invalid platform admin role.");

            if (!await _roleManager.RoleExistsAsync(request.Role))
                throw new ApiException("Role does not exist.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);

            if (existingUserByEmail != null)
            {
                _logger.LogWarning(
                    "Platform admin creation failed — email already exists. Email: {Email}",
                    request.Email);

                throw new ApiException("Email is already registered.");
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(request.UserName);

            if (existingUserByUsername != null)
            {
                _logger.LogWarning(
                    "Platform admin creation failed — username already taken. UserName: {UserName}",
                    request.UserName);

                throw new ApiException("Username is already taken.");
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var temporaryPasswordExpiresAt = DateTime.UtcNow.AddHours(24);

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FullName = request.FullName,

                UserType = UserTypes.Platform,
                TenantId = null,

                IsActive = true,
                EmailConfirmed = true,

                MustChangePassword = true,
                TemporaryPasswordExpiresAt = temporaryPasswordExpiresAt,

                CreatedAt = DateTime.UtcNow
            };

            var userCreated = false;

            try
            {
                _logger.LogInformation(
                    "Creating platform admin. Email: {Email}, Role: {Role}, CreatedBy: {CreatedBy}",
                    request.Email,
                    request.Role,
                    currentUser.Id);

                var createResult = await _userManager.CreateAsync(
                    user,
                    temporaryPassword);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));

                    _logger.LogError(
                        "Platform admin creation failed for {Email}. Errors: {Errors}",
                        request.Email,
                        errors);

                    throw new ApiException($"Platform admin creation failed: {errors}");
                }

                userCreated = true;

                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    request.Role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));

                    _logger.LogError(
                        "Adding platform role {Role} failed for {Email}. Errors: {Errors}",
                        request.Role,
                        request.Email,
                        errors);

                    throw new ApiException($"Adding role failed: {errors}");
                }

                await _emailService.SendPlatformAdminInvitationEmailAsync(
                    user.Email!,
                    user.FullName,
                    temporaryPassword,
                    temporaryPasswordExpiresAt,
                    cancellationToken);

                _logger.LogInformation(
                    "Platform admin created successfully. UserId: {UserId}, Email: {Email}, Role: {Role}",
                    user.Id,
                    user.Email,
                    request.Role);

                return new CreatePlatformAdminResult
                {
                    Email = user.Email!,
                    Role = request.Role
                };
            }
            catch
            {
                if (userCreated)
                {
                    var deleteResult = await _userManager.DeleteAsync(user);

                    if (!deleteResult.Succeeded)
                    {
                        var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));

                        _logger.LogError(
                            "Failed to delete platform admin after creation failure. UserId: {UserId}, Errors: {Errors}",
                            user.Id,
                            errors);
                    }
                }

                throw;
            }
        }
        private class PlatformUserValidationResult
        {
            public ApplicationUser User { get; set; } = null!;

            public bool IsSuperAdmin { get; set; }

            public bool IsPlatformAdmin { get; set; }
        }

        //Helpers
        private async Task<PlatformUserValidationResult> ValidateCurrentPlatformUserAsync(string? currentUserId,string requestedRole)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new ApiException("User is not authenticated.");

            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser == null)
            {
                _logger.LogWarning(
                    "Platform admin creation failed — current user not found. UserId: {UserId}",
                    currentUserId);

                throw new ApiException("Current user not found.");
            }

            if (!currentUser.IsActive)
                throw new ApiException("This account has been deactivated.");

            if (currentUser.UserType != UserTypes.Platform)
                throw new ApiException("Only platform users can create platform admins.");

            if (currentUser.TenantId.HasValue)
                throw new ApiException("Platform users cannot be linked to a tenant.");

            var isSuperAdmin = await _userManager.IsInRoleAsync(
                currentUser,
                PlatformRoles.SuperAdmin);

            var isPlatformAdmin = await _userManager.IsInRoleAsync(
                currentUser,
                PlatformRoles.Admin);

            if (!isSuperAdmin && !isPlatformAdmin)
                throw new ApiException("User does not have privileges to create platform admins.");

            if (!isSuperAdmin && requestedRole == PlatformRoles.SuperAdmin)
                throw new ApiException("Only SuperAdmin can create another SuperAdmin.");

            return new PlatformUserValidationResult
            {
                User = currentUser,
                IsSuperAdmin = isSuperAdmin,
                IsPlatformAdmin = isPlatformAdmin
            };
        }
        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@$?_-";
            const string all = upper + lower + digits + special;

            // ✅ Call the method directly — no delegate assignment
            var chars = new List<char>
            {
                upper  [RandomNumberGenerator.GetInt32(upper.Length)],
                lower  [RandomNumberGenerator.GetInt32(lower.Length)],
                digits [RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)]
            };

            while (chars.Count < 12)
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            // ✅ Shuffle using the same secure generator
            return new string(chars
                .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                .ToArray());
        }
       
    }
}