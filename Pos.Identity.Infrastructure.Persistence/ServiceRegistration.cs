using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pos.Identity.Domain.Constants;
using Pos.Identity.Domain.Models;
using Pos.Identity.Infrastructure.Persistence.Constants;
using Pos.Identity.Infrastructure.Persistence.Context;
using Pos.Identity.Infrastructure.Persistence.Seeders;
using SendGrid;
using static OpenIddict.Abstractions.OpenIddictConstants;

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

        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services,IConfiguration configuration)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        AuthenticationSchemes.ApplicationCookie;

                    options.DefaultSignInScheme =
                        AuthenticationSchemes.ApplicationCookie;

                    options.DefaultChallengeScheme =
                        AuthenticationSchemes.ApplicationCookie;
                })
                .AddCookie(
                    AuthenticationSchemes.ApplicationCookie,
                    options =>
                    {
                        options.LoginPath = "/account/login";
                        options.LogoutPath = "/account/logout";

                        options.Cookie.Name =
                            "__Host-pos-identity";

                        options.Cookie.HttpOnly = true;

                        options.Cookie.SecurePolicy =
                            CookieSecurePolicy.Always;

                        options.Cookie.SameSite =
                            SameSiteMode.Lax;

                        options.SlidingExpiration = true;

                        options.ExpireTimeSpan =
                            TimeSpan.FromHours(8);
                    })
                .AddCookie(
                    AuthenticationSchemes.ExternalCookie,
                    options =>
                    {
                        options.Cookie.Name =
                            "__Host-pos-external";

                        options.Cookie.HttpOnly = true;

                        options.Cookie.SecurePolicy =
                            CookieSecurePolicy.Always;

                        options.Cookie.SameSite =
                            SameSiteMode.Lax;

                        options.ExpireTimeSpan =
                            TimeSpan.FromMinutes(10);
                    });

            return services;
        }

        public static IServiceCollection AddOpenIddictServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore()
                .UseDbContext<ApplicationDbContext>();
        })
        .AddServer(options =>
        {
            options.SetAuthorizationEndpointUris(
                "/connect/authorize");

            options.SetTokenEndpointUris(
                "/connect/token");

            options.SetEndSessionEndpointUris(
                "/connect/logout");

            options.AllowAuthorizationCodeFlow();
            options.AllowRefreshTokenFlow();

            // Authorization Code requests must use PKCE.
            options.RequireProofKeyForCodeExchange();

            options.RegisterScopes(
                Scopes.OpenId,
                Scopes.Profile,
                Scopes.Email,
                Scopes.Roles,
                Scopes.OfflineAccess);

            options.DisableAccessTokenEncryption();

            // Development only.
            options.AddDevelopmentEncryptionCertificate();
            options.AddDevelopmentSigningCertificate();

            options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableEndSessionEndpointPassthrough();
        });

            return services;
        }


        public static IServiceCollection AddSocialAuthentication(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddAuthentication()
                .AddGoogle(AuthenticationSchemes.Google, options =>
                {
                    // pulled from appsettings or secrets
                    options.ClientId = configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = configuration["Authentication:Google:ClientSecret"];

                    options.SignInScheme =
                    AuthenticationSchemes.ExternalCookie;

                    // These are the user fields we want Google to return
                    options.Scope.Add("email");
                    options.Scope.Add("profile");

                    // Map Google's claim names to standard OIDC claim names
                    options.ClaimActions.MapJsonKey(Claims.Subject, "sub");
                    options.ClaimActions.MapJsonKey(Claims.Email, "email");
                    options.ClaimActions.MapJsonKey(Claims.Name, "name");
                })
                .AddFacebook(AuthenticationSchemes.Facebook, options =>
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