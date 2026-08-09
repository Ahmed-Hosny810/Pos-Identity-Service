using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pos.Identity.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Seeders
{
    public class IdentityRoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<IdentityRoleSeeder> _logger;

        public IdentityRoleSeeder(
            RoleManager<IdentityRole> roleManager,
            ILogger<IdentityRoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            var roles = new[]
            {
                PlatformRoles.SuperAdmin,
                PlatformRoles.Admin,

                TenantRoles.TenantOwner,
                TenantRoles.Manager,
                TenantRoles.Cashier,
                TenantRoles.InventoryStaff
            };

            foreach (var role in roles.Distinct())
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    _logger.LogInformation(
                        "Role already exists: {Role}",
                        role);

                    continue;
                }

                var result = await _roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    _logger.LogError(
                        "Failed to seed role {Role}. Errors: {Errors}",
                        role,
                        errors);

                    throw new InvalidOperationException(
                        $"Failed to seed role {role}: {errors}");
                }

                _logger.LogInformation(
                    "Role seeded successfully: {Role}",
                    role);
            }
        }
    }
}
