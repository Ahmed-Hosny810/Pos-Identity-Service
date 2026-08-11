using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pos.Identity.Application.Dtos;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Features.TenantUsers.Commands.CreateCommand;
using Pos.Identity.Application.Features.TenantUsers.DTOS;
using Pos.Identity.Application.Interfaces.Clients;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Domain.Constants;
using Pos.Identity.Domain.Models;
using System.Security.Cryptography;

namespace Pos.Identity.Infrastructure.Shared.Services
{
    public class TenantUserService : ITenantUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<TenantUserService> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITenantBillingClient _tenantBillingClient;
        private readonly IEmailService _emailService;

        public TenantUserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<TenantUserService> logger,
            ICurrentUserService currentUserService, 
            ITenantBillingClient tenantBillingClient,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _currentUserService = currentUserService;
            _tenantBillingClient = tenantBillingClient;
            _emailService = emailService;
        }

        public async Task<CreateTenantUserResult> CreateTenantUserAsync(CreateTenantUserCommand request,CancellationToken cancellationToken)
        {
            var tenantAdminId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(tenantAdminId))
                throw new ApiException("User is not authenticated.");

            var tenantAdmin = await _userManager.FindByIdAsync(tenantAdminId);

            if (tenantAdmin == null)
            {
                _logger.LogWarning(
                    "Tenant admin with ID {TenantAdminId} not found",
                    tenantAdminId);

                throw new ApiException("Tenant admin not found.");
            }

            if (!tenantAdmin.IsActive)
                throw new ApiException("This account has been deactivated.");

            if (tenantAdmin.UserType != UserTypes.Tenant)
                throw new ApiException("Only tenant users can create tenant staff users.");

            if (!tenantAdmin.TenantId.HasValue || tenantAdmin.TenantId.Value == Guid.Empty)
            {
                _logger.LogWarning(
                    "Tenant admin with ID {TenantAdminId} does not have a valid tenant ID",
                    tenantAdminId);

                throw new ApiException("Tenant admin does not have a valid tenant ID.");
            }

            var tenantId = tenantAdmin.TenantId.Value;

            var isTenantOwner = await _userManager.IsInRoleAsync(
                tenantAdmin,
                TenantRoles.TenantOwner);

            var isTenantAdmin = await _userManager.IsInRoleAsync(
                tenantAdmin,
                TenantRoles.Admin);

            if (!isTenantOwner && !isTenantAdmin)
            {
                _logger.LogWarning(
                    "User with ID {TenantAdminId} does not have privileges to add tenant users",
                    tenantAdminId);

                throw new ApiException("User does not have the privileges to add a tenant user.");
            }

            var allowedRoles = new[]
            {
                 TenantRoles.Admin,
                 TenantRoles.Cashier,
                 TenantRoles.InventoryStaff
            };

            if (!allowedRoles.Contains(request.Role))
                throw new ApiException("Invalid tenant user role.");

            if (!isTenantOwner && request.Role == TenantRoles.Admin)
                throw new ApiException("Only tenant owner can create tenant admins.");

            if (!await _roleManager.RoleExistsAsync(request.Role))
                throw new ApiException("Role does not exist.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);

            if (existingUserByEmail != null)
            {
                _logger.LogWarning(
                    "User with email {Email} already exists",
                    request.Email);

                throw new ApiException("Email is already registered.");
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(request.UserName);

            if (existingUserByUsername != null)
            {
                _logger.LogWarning(
                    "Registration failed — username already taken: {UserName}",
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
                TenantId = tenantId,
                UserType = UserTypes.Tenant,
                IsActive = true,
                EmailConfirmed = true,
                MustChangePassword = true,
                TemporaryPasswordExpiresAt = temporaryPasswordExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            var isCashier = request.Role == TenantRoles.Cashier;
            var cashierUsageIncreased = false;
            var userCreated = false;

            try
            {
                if (isCashier)
                {
                    await _tenantBillingClient.IncreaseCashierUsageAsync(
                        new IncreaseCashierUsageRequest
                        {
                            TenantId = tenantId
                        },
                        cancellationToken);

                    cashierUsageIncreased = true;
                }

                _logger.LogInformation(
                    "Creating tenant user for TenantId {TenantId} with Email {Email} and Role {Role}",
                    tenantId,
                    request.Email,
                    request.Role);

                var createResult = await _userManager.CreateAsync(user, temporaryPassword);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));

                    _logger.LogError(
                        "Tenant user creation failed for {Email}. Errors: {Errors}",
                        request.Email,
                        errors);

                    throw new ApiException($"Tenant user creation failed: {errors}");
                }

                userCreated = true;

                var roleResult = await _userManager.AddToRoleAsync(user, request.Role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));

                    _logger.LogError(
                        "Adding role {Role} failed for {Email}. Errors: {Errors}",
                        request.Role,
                        request.Email,
                        errors);

                    throw new ApiException($"Adding role failed: {errors}");
                }

                await _emailService.SendTenantUserInvitationEmailAsync(
                    user.Email!,
                    user.FullName,
                    temporaryPassword,
                    temporaryPasswordExpiresAt);

                _logger.LogInformation(
                    "Tenant user created successfully. UserId: {UserId}, TenantId: {TenantId}, Role: {Role}",
                    user.Id,
                    tenantId,
                    request.Role);

                return new CreateTenantUserResult
                {
                    UserId = user.Id,
                    TenantId = tenantId,
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
                            "Failed to delete tenant user after creation failure. UserId: {UserId}, Errors: {Errors}",
                            user.Id,
                            errors);
                    }
                }

                if (cashierUsageIncreased)
                {
                    try
                    {
                        await _tenantBillingClient.DecreaseCashierUsageAsync(
                            new DecreaseCashierUsageRequest
                            {
                                TenantId = tenantId
                            },
                            cancellationToken);
                    }
                    catch (Exception rollbackException)
                    {
                        _logger.LogError(
                            rollbackException,
                            "Failed to rollback cashier usage for TenantId {TenantId}",
                            tenantId);
                    }
                }

                throw;
            }
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