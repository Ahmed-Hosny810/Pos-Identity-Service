using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pos.Identity.Application.Dtos;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Clients;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Constants;
using Pos.Identity.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.TenantOnboarding.Commands
{
    public class CreateTenantOnboardingCommand:IRequest<Response<CreateTenantResult>>
    {
        public string NameAr { get; set; } = null!;

        public string NameEn { get; set; } = null!;

        public string BusinessTypeCode { get; set; } = null!;

        public string CurrencyCode { get; set; } = "EGP";

        public string InventoryMode { get; set; } = "TrackStock";

        public string PlanCode { get; set; } = null!;
    }

    public class CreateTenantOnboardingCommandHandler : IRequestHandler<CreateTenantOnboardingCommand, Response<CreateTenantResult>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantBillingClient _tenantBillingClient;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateTenantOnboardingCommandHandler> _logger;

        public CreateTenantOnboardingCommandHandler(UserManager<ApplicationUser> userManager,ITenantBillingClient tenantBillingClient,
            ICurrentUserService currentUserService,ILogger<CreateTenantOnboardingCommandHandler> logger)
        {
            _userManager = userManager;
            _tenantBillingClient = tenantBillingClient;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Response<CreateTenantResult>> Handle(CreateTenantOnboardingCommand request,CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new ApiException("User is not authenticated.");

            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
                throw new ApiException("User not found.");

            if (!user.IsActive)
                throw new ApiException("This account has been deactivated.");

            if (user.UserType != UserTypes.PendingTenant)
                throw new ApiException("Only pending tenant users can start tenant onboarding.");

            if (user.TenantId.HasValue)
                throw new ApiException("User is already linked to a tenant.");

            _logger.LogInformation(
                "Starting tenant onboarding. UserId: {UserId}, Email: {Email}",
                user.Id,
                user.Email);

            var tenantResult = await _tenantBillingClient.CreateTenantAsync(
                new CreateTenantRequest
                {
                    NameAr = request.NameAr,
                    NameEn = request.NameEn,
                    BusinessTypeCode = request.BusinessTypeCode,
                    CurrencyCode = request.CurrencyCode,
                    InventoryMode = request.InventoryMode,
                    PlanCode = request.PlanCode
                },
                cancellationToken);

            user.UserType = UserTypes.Tenant;
            user.TenantId = tenantResult.TenantId;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));

                _logger.LogError(
                    "Failed to update user tenant context. UserId: {UserId}, TenantId: {TenantId}, Errors: {Errors}",
                    user.Id,
                    tenantResult.TenantId,
                    errors);

                throw new ApiException($"Failed to update user tenant context: {errors}");
            }

            if (!await _userManager.IsInRoleAsync(user, TenantRoles.TenantOwner))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, TenantRoles.TenantOwner);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));

                    _logger.LogError(
                        "Failed to assign TenantOwner role. UserId: {UserId}, TenantId: {TenantId}, Errors: {Errors}",
                        user.Id,
                        tenantResult.TenantId,
                        errors);

                    throw new ApiException($"Failed to assign TenantOwner role: {errors}");
                }
            }

            _logger.LogInformation(
                "Tenant onboarding completed. UserId: {UserId}, TenantId: {TenantId}",
                user.Id,
                tenantResult.TenantId);

            return new Response<CreateTenantResult>(
                new CreateTenantResult
                {
                    TenantId = tenantResult.TenantId,
                },
                "Tenant onboarding completed successfully.");
        }
    }
}
