using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Infrastructure.Shared.Services;
using Pos.Identity.Infrastructure.Shared.Settings;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSharedInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<ISendGridClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
                return new SendGridClient(settings.SendGridApiKey);
            });

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
            services.AddScoped<IRoleService, RoleService>();

            return services;
        }
    }
}
