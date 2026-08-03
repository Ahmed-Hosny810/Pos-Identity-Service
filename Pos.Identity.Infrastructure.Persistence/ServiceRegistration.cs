using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SendGrid;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Pos.Identity.Infrastructure.Persistence.Context;
using Pos.Identity.Domain.Models;
using Pos.Identity.Infrastructure.Persistence.Seeders;
using Pos.Identity.Domain.Constants;

namespace Pos.Identity.Infrastructure.Persistence
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddDbContext<ApplicationDbContext>(opt =>
            {
                opt.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                opt.UseOpenIddict(); 
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            }).AddEntityFrameworkStores<ApplicationDbContext>()
              .AddDefaultTokenProviders();

            services.AddHostedService<OpenIddictSeeder>();
            
            return services;
        }

        public static IServiceCollection AddOpenIddictServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpenIddict()
         .AddCore(options =>
         {
             options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
         })
         .AddServer(options =>
         {
             options.SetTokenEndpointUris("/connect/token")
                        .SetAuthorizationEndpointUris("/connect/authorize");

             options.AllowPasswordFlow();
             options.AllowRefreshTokenFlow();
             options.AllowAuthorizationCodeFlow();

             options.RegisterScopes(
                 Scopes.OpenId,
                 Scopes.Email,
                 Scopes.Profile,
                 Scopes.Roles,
                 "api");

             options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

             options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .EnableAuthorizationEndpointPassthrough();
         });

            return services;
        }


        public static IServiceCollection AddSocialAuthentication(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    // pulled from appsettings or secrets
                    options.ClientId = configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = configuration["Authentication:Google:ClientSecret"];

                    // These are the user fields we want Google to return
                    options.Scope.Add("email");
                    options.Scope.Add("profile");

                    // Map Google's claim names to standard OIDC claim names
                    options.ClaimActions.MapJsonKey(Claims.Subject, "sub");
                    options.ClaimActions.MapJsonKey(Claims.Email, "email");
                    options.ClaimActions.MapJsonKey(Claims.Name, "name");
                })
                .AddFacebook(options =>
                {
                    options.AppId = configuration["Authentication:Facebook:AppId"];
                    options.AppSecret = configuration["Authentication:Facebook:AppSecret"];

                    options.Fields.Add("email");
                    options.Fields.Add("name");
                });

            return services;
        }

        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();    
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = [
             PlatformRoles.Admin
            ];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}