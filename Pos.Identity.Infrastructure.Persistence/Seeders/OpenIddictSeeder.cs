using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Pos.Identity.Infrastructure.Persistence.Seeders
{
    public class OpenIddictSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public OpenIddictSeeder(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var manager = scope.ServiceProvider
                .GetRequiredService<IOpenIddictApplicationManager>();

            if (await manager.FindByClientIdAsync("angular-client", cancellationToken) is null)
            {
                await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "angular-client",
                    DisplayName = "Angular SPA",
                    Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.GrantTypes.Password,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.Prefixes.Scope + "api",
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Roles
                    }
                }, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
