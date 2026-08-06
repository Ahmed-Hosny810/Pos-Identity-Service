using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Pos.Identity.Infrastructure.Persistence.Seeders;

public sealed class OpenIddictSeeder : IHostedService
{
    private const string ClientId =
        "pos-angular-client";

    private readonly IServiceProvider _serviceProvider;

    public OpenIddictSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope =_serviceProvider.CreateAsyncScope();

        var manager =scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var application =await manager.FindByClientIdAsync(ClientId,cancellationToken);

        if (application is not null)
        {
            return;
        }

        var descriptor =
            new OpenIddictApplicationDescriptor
            {
                ClientId = ClientId,
                DisplayName = "POS Angular SPA",

                // Angular cannot protect a client secret.
                ClientType = ClientTypes.Public,

                // Your application trusts this internal client,
                // so no consent page is required.
                ConsentType = ConsentTypes.Implicit,

                RedirectUris =
                {
                    new Uri(
                        "https://localhost:4200/auth/callback")
                },

                Permissions =
                {
                    
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,

                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,

                    // Allows response_type=code.
                    Permissions.ResponseTypes.Code,

                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,

                },

                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            };

        await manager.CreateAsync(
            descriptor,
            cancellationToken);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}