using OpenIddict.Validation.AspNetCore;
using Pos.Identity.Application.Common.Security;
using Pos.Identity.Domain.Constants;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Pos.Auth.WebApi.Policies
{
    public static class AppPolicies
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("BearerOnly", policy =>
                {
                    policy.AuthenticationSchemes.Add(
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy("PendingTenantOnly", policy =>
                {
                    policy.AuthenticationSchemes.Add(
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(CustomClaimTypes.UserType, UserTypes.PendingTenant);
                });

                options.AddPolicy("TenantUserOnly", policy =>
                {
                    policy.AuthenticationSchemes.Add(
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(CustomClaimTypes.UserType, UserTypes.Tenant);
                });

                options.AddPolicy("CanManageTenantUsers", policy =>
                {
                    policy.AuthenticationSchemes.Add(
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireAssertion(context =>
                    {
                        var userType = context.User.FindFirst(CustomClaimTypes.UserType)?.Value;

                        var roles = context.User
                            .FindAll(Claims.Role)
                            .Select(x => x.Value)
                            .ToList();

                        return userType == UserTypes.Tenant &&
                               (
                                   roles.Contains(TenantRoles.TenantOwner) ||
                                   roles.Contains(TenantRoles.Admin)
                               );
                    });
                });
                options.AddPolicy("CanManagePlatformAdmins", policy =>
                {
                    policy.AuthenticationSchemes.Add(
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireAssertion(context =>
                    {
                        var userType = context.User
                            .FindFirst(CustomClaimTypes.UserType)?.Value;

                        var roles = context.User
                            .FindAll(Claims.Role)
                            .Select(x => x.Value)
                            .ToList();

                        return userType == UserTypes.Platform &&
                               (
                                   roles.Contains(PlatformRoles.SuperAdmin) ||
                                   roles.Contains(PlatformRoles.Admin)
                               );
                    });
                });

                options.AddPolicy("CanResendInvitations", policy =>
                {
                    policy.AuthenticationSchemes.Add(
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireAssertion(context =>
                    {
                        var userType = context.User
                            .FindFirst(CustomClaimTypes.UserType)?.Value;

                        var roles = context.User
                            .FindAll(Claims.Role)
                            .Select(x => x.Value)
                            .ToList();

                        var isPlatformUser = userType == UserTypes.Platform &&
                                             (
                                                 roles.Contains(PlatformRoles.SuperAdmin) ||
                                                 roles.Contains(PlatformRoles.Admin)
                                             );

                        var isTenantUser = userType == UserTypes.Tenant &&
                                           (
                                               roles.Contains(TenantRoles.TenantOwner) ||
                                               roles.Contains(TenantRoles.Admin)
                                           );

                        return isPlatformUser || isTenantUser;
                    });
                });

            });

            return services;
        }
    }
}
