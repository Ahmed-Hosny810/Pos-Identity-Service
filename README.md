# Vendora Identity Service

Identity and access-management service for the Vendora multi-tenant SaaS platform. It is both:

- the OpenID Connect (OIDC) authorization server that authenticates users and issues tokens; and
- the identity-management API for registration, onboarding, roles, tenant users, platform administrators, invitations, password recovery, and logout/session state.

The service owns identity data. Business services such as Tenant Billing, Inventory, Catalog, and Sales remain separate resource APIs and trust access tokens issued by this service.

> Endpoint names below reflect the current design. Keep this README synchronized with controllers and API versioning as implementation evolves.

## Responsibilities

- Register users and confirm email addresses.
- Authenticate local users and, when enabled, external Google/Facebook users.
- Host OIDC authorization, token, and end-session endpoints.
- Issue short-lived signed access tokens and longer-lived refresh tokens.
- Publish identity claims such as subject, email, roles, `user_type`, and `tenant_id`.
- Support the pending-tenant onboarding lifecycle.
- Create and manage tenant users without crossing tenant boundaries.
- Create and manage platform administrators using platform role rules.
- Send confirmation, password-reset, and invitation emails through SendGrid.
- Enforce temporary-password expiry and mandatory first-login password changes.
- Track MVP login state with `IsLoggedIn` and `LastAccessedAt`.

The service does not own tenant billing, subscriptions, inventory, catalog, or sales data.

## Project Structure

```text
Pos.Identity.Domain
├── Constants                 # User types, roles, custom claim names
└── Models                    # ApplicationUser and domain models

Pos.Identity.Application
├── Exceptions
├── Features                  # Commands, queries, handlers, validators
├── Interfaces
│   └── Services              # Authentication, users, invitations, email, current user
└── Wrappers                  # Standard API response models

Pos.Identity.Infrastructure.Persistence
├── Context                   # ApplicationDbContext
├── Migrations
├── Seeders                   # Roles and OpenIddict clients/applications
└── ServiceRegistration.cs    # SQL Server, Identity, OpenIddict persistence

Pos.Identity.Infrastructure.Shared
├── Services                  # Email, invitations, platform/tenant user services
├── Settings                  # Email and integration options
└── Clients                   # Calls to Tenant Billing when required

Pos.Identity.WebApi
├── Controllers               # REST and OIDC endpoint handlers
├── Middlewares               # Error handling and user activity tracking
├── Program.cs
└── appsettings*.json

Pos.Identity.IntegrationTests # Service/API integration tests
```

## Tech Stack

- .NET / ASP.NET Core Web API
- ASP.NET Core Identity with `ApplicationUser` and `IdentityRole`
- Entity Framework Core and SQL Server
- OpenIddict server, EF Core stores, and validation stack
- MediatR and FluentValidation
- SendGrid for transactional email
- Google and Facebook authentication when configured
- xUnit and FluentAssertions for tests
- SQL Server LocalDB or SQL Server Testcontainers for production-like integration tests

## Authentication and OIDC Flow

The recommended client flow is Authorization Code with PKCE:

```text
Browser/client
  → /connect/authorize
  → Identity login page and application cookie
  → authorization code
  → /connect/token with code verifier
  → access token + optional refresh token
  → Authorization: Bearer <access-token> on APIs
```

OpenIddict endpoints:

| Endpoint | Purpose |
|---|---|
| `GET/POST /connect/authorize` | Starts or completes the authorization-code flow. |
| `POST /connect/token` | Exchanges an authorization code or refresh token for tokens. |
| `GET/POST /connect/logout` | Ends the Identity browser session and clears its cookie when implemented. |

The end-session endpoint does not automatically expire or revoke access tokens already issued. Frontend token removal, server-side session checks, and token revocation are separate concerns.

### Token claims

Access tokens should contain only claims required by resource services, normally:

- `sub`: ASP.NET Identity user ID
- `email` and profile claims where needed
- `role`: one or more assigned roles
- `user_type`: `PendingTenant`, `Tenant`, or `Platform`
- `tenant_id`: present only for tenant users

Do not place secrets, temporary passwords, or unnecessary personal data in tokens.

## Cookies vs Bearer Tokens

Cookies and bearer tokens have distinct jobs:

| Mechanism | Used for | Not used for |
|---|---|---|
| `ApplicationCookie` | Browser session at Identity, login UI, `/connect/authorize` | Calling business APIs |
| `ExternalCookie` | Short-lived Google/Facebook callback state | Application authorization |
| Bearer access token | Identity REST APIs and every other microservice API | Maintaining the Identity login page session |

Identity keeps the application cookie as its default browser authentication scheme. Its `/api/...` controllers must explicitly require a bearer policy such as `BearerOnly`, `PendingTenantOnly`, or `CanManageTenantUsers`; plain `[Authorize]` would otherwise select the cookie scheme.

Other microservices do not use Identity cookies. They validate bearer tokens against the Identity issuer using OpenID Connect discovery.

## OpenIddict Configuration

Representative server configuration:

```csharp
services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetTokenEndpointUris("/connect/token");
        options.SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow();
        options.AllowRefreshTokenFlow();
        options.RequireProofKeyForCodeExchange();

        options.RegisterScopes("openid", "profile", "email", "roles", "offline_access");
        options.DisableAccessTokenEncryption();
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(5));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

        // Development only; replace with managed production certificates.
        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });
```

`UseLocalServer()` is only for bearer-protected APIs hosted in the same application as the OpenIddict server. Other services use the issuer and discovery:

```csharp
services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(configuration["Services:Identity:Issuer"]!);
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });
```

Use asymmetric signing certificates: Identity holds the private key and resource services obtain public signing keys through discovery. Do not distribute a shared symmetric signing secret to every microservice. Signed, unencrypted JWT access tokens are the practical MVP choice; TLS still protects tokens in transit.

Seed all roles and every allowed OIDC client, redirect URI, post-logout redirect URI, grant type, scope, and endpoint permission explicitly.

## User Types and Roles

### User types

| User type | Tenant ID | Meaning |
|---|---:|---|
| `PendingTenant` | No | Registered and confirmed, but tenant onboarding is not complete. |
| `Tenant` | Required | Belongs to exactly one tenant. |
| `Platform` | No | Operates the Vendora platform, not a customer tenant. |

### Roles

```text
Platform
├── SuperAdmin
└── Admin

Tenant
├── TenantOwner
├── Admin
├── Cashier
└── InventoryStaff
```

Important invariants:

- Platform users must have `TenantId == null`.
- Tenant users must have a `TenantId` and may manage only users in that tenant.
- Only a `SuperAdmin` may create or manage another `SuperAdmin`.
- A tenant owner may manage tenant admins and staff.
- A tenant admin must not manage a tenant owner and should not elevate users beyond its authority.
- Role checks must run on the server; never trust role or tenant values supplied by the frontend.

## Main APIs

The exact request/response DTOs live with their application features.

| Method and route | Authentication | Purpose |
|---|---|---|
| `POST /api/v1/authentication/register` | Public | Register an initial pending-tenant user. |
| `GET /api/v1/authentication/confirm-email` | Public, token | Confirm the registered email address. |
| `POST /api/v1/authentication/login` | Public | Validate credentials and begin the application/OIDC sign-in flow. |
| `POST /api/v1/authentication/forgot-password` | Public | Send a password-reset email without revealing account existence. |
| `POST /api/v1/authentication/reset-password` | Public, token | Reset a forgotten password. |
| `POST /api/v1/authentication/change-temporary-password` | Bearer | Replace a valid temporary password on first login. |
| `POST /api/v1/authentication/logout` | `BearerOnly` | Clear application login/activity state; the client then removes its tokens. |
| `POST /api/v1/tenant-onboarding` | `PendingTenantOnly` | Complete tenant onboarding in coordination with Tenant Billing. |
| `POST /api/v1/tenant-users` | `CanManageTenantUsers` | Create and invite a tenant staff user. |
| `POST /api/v1/platform-admins` | Platform admin policy | Create and invite a platform admin. |
| `POST /api/v1/users/{userId}/resend-invitation` | Bearer plus service authorization | Replace and resend an invitation by target user ID. |

Avoid returning raw Identity errors, tokens, temporary passwords, or internal security state in logs or normal API responses.

## Tenant Onboarding Flow

```text
1. User registers with email/password.
2. Identity creates a PendingTenant user and sends confirmation email.
3. User confirms the email.
4. Client obtains a token containing user_type=PendingTenant.
5. Client calls POST /api/v1/tenant-onboarding.
6. Identity coordinates tenant creation with Tenant Billing.
7. On success, Identity assigns TenantId, changes UserType to Tenant,
   and assigns TenantOwner.
8. The client obtains a fresh token so the new tenant and role claims are present.
```

Treat steps spanning Identity and Tenant Billing as a distributed workflow. Use an idempotency key and a durable retry/compensation strategy as the system matures. Do not leave the identity marked as fully onboarded if tenant creation failed.

## Tenant and Platform User Management

Tenant user creation must:

1. Authenticate the caller using a bearer token.
2. Load the caller and verify it is active, is type `Tenant`, has a tenant ID, and has a management role.
3. Reject duplicate email or username.
4. Validate the requested role and the caller's authority to grant it.
5. Create the target with the caller's tenant ID; never accept an arbitrary tenant ID from the request.
6. Assign the role, initialize invitation state, and send the invitation.

Platform administrator creation follows the same shape but requires `UserType == Platform` and `TenantId == null`. A platform `Admin` cannot create a `SuperAdmin`; only a `SuperAdmin` can.

Keep business authorization in `TenantUserService` and `PlatformAdminService`. Keep reusable password/invitation mechanics in `IUserInvitationService`.

## Temporary-Password Invitation Flow

For tenant staff and platform administrators:

```text
Admin creates user
→ generate a cryptographically secure 12-character temporary password
→ store only its ASP.NET Identity hash
→ EmailConfirmed = true
→ MustChangePassword = true
→ TemporaryPasswordExpiresAt = UtcNow + 24 hours
→ send the plain temporary password once by invitation email
→ invited user logs in
→ API requires change-temporary-password before normal use
→ verify current temporary password and expiry
→ ChangePasswordAsync(currentTemporaryPassword, newPassword)
→ MustChangePassword = false
→ TemporaryPasswordExpiresAt = null
```

Use `ChangePasswordAsync` for the authenticated first-login change. Reserve `ResetPasswordAsync` for the forgot-password flow that uses a reset token.

The invitation operation crosses database and email boundaries. If email delivery fails after the user is created, preserve a recoverable invited state and allow an authorized resend instead of exposing the temporary password in an API response.

## Resend Invitation

Use one generic endpoint keyed by `userId` and one shared invitation service. Before resending:

- require the current user to be authenticated and active;
- require the target user to exist and be active;
- require `MustChangePassword == true`;
- enforce platform/tenant boundaries and role hierarchy;
- for tenant callers, require the same `TenantId`;
- prevent tenant flows from targeting platform users and vice versa.

The service generates a new temporary password, removes/replaces the old password hash, resets the 24-hour expiry, and sends a new email. The original plain password cannot be resent because it is never stored. Once `MustChangePassword` is false, use forgot-password rather than resend-invitation.

## Concurrent Login MVP Behavior

The current MVP rule uses `IsLoggedIn` and `LastAccessedAt`:

- Successful login sets `IsLoggedIn = true` and `LastAccessedAt = UtcNow`.
- Authenticated bearer activity updates `LastAccessedAt`, preferably no more than once per minute to limit database writes.
- Inactivity alone never logs a user out.
- On a new login attempt, if the existing session was active within the last 20 minutes, reject the new login.
- If the previous activity is older than 20 minutes, allow the new login and replace the logical session.
- Logout sets `IsLoggedIn = false`, clears `LastAccessedAt`, and the frontend deletes its access and refresh tokens.

This is an MVP concurrency guard, not complete token revocation. A signed JWT remains cryptographically valid until its `exp` unless every resource service checks session state or uses OpenIddict introspection/revocation. For stronger single-session enforcement, add a `CurrentSessionId`, include it in tokens, validate it on protected requests, and/or adopt introspection.

## Configuration

Keep non-secret defaults in `appsettings.json` and secrets in user-secrets, environment variables, or a production secret manager.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PosIdentity;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Identity": {
    "Issuer": "https://localhost:7001/",
    "AccessTokenMinutes": 5,
    "RefreshTokenDays": 7,
    "TemporaryPasswordHours": 24,
    "ConcurrentLoginWindowMinutes": 20
  },
  "EmailSettings": {
    "ApiKey": "set-with-user-secrets-or-environment-variable",
    "FromEmail": "no-reply@example.com",
    "FromName": "Vendora",
    "AppBaseUrl": "https://localhost:7001",
    "ApiVersion": "v1",
    "ConfirmationTokenExpiryHours": 24,
    "PasswordResetTokenExpiryHours": 1
  },
  "Authentication": {
    "Google": {
      "ClientId": "optional",
      "ClientSecret": "optional"
    },
    "Facebook": {
      "AppId": "optional",
      "AppSecret": "optional"
    }
  },
  "Services": {
    "TenantBilling": {
      "BaseUrl": "https://localhost:7002"
    }
  }
}
```

Common secret setup:

```bash
dotnet user-secrets init --project Pos.Identity.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project Pos.Identity.WebApi
dotnet user-secrets set "EmailSettings:ApiKey" "<sendgrid-key>" --project Pos.Identity.WebApi
```

Use HTTPS and an issuer URL that exactly matches the externally visible Identity URL. Configure allowed CORS origins and OpenIddict redirect URIs explicitly; do not use wildcards in production.

## Database Migrations

`ApplicationDbContext` contains both ASP.NET Identity and OpenIddict entities, so migrations must cover both schemas.

From the solution directory, adjust project paths if necessary:

```bash
dotnet ef migrations add InitialIdentity \
  --project Pos.Identity.Infrastructure.Persistence \
  --startup-project Pos.Identity.WebApi \
  --context ApplicationDbContext

dotnet ef database update \
  --project Pos.Identity.Infrastructure.Persistence \
  --startup-project Pos.Identity.WebApi \
  --context ApplicationDbContext
```

Run migrations as a controlled deployment step in production. Avoid `EnsureCreated()` outside disposable tests. Ensure the startup seeder is idempotent and creates all platform roles, tenant roles, and registered OpenIddict clients.

## Running Locally

Prerequisites:

- matching .NET SDK
- SQL Server, SQL Server Express, or LocalDB
- a SendGrid key, or a development email implementation
- HTTPS development certificate
- Tenant Billing running when testing the full onboarding workflow

```bash
dotnet restore
dotnet dev-certs https --trust
dotnet ef database update \
  --project Pos.Identity.Infrastructure.Persistence \
  --startup-project Pos.Identity.WebApi
dotnet run --project Pos.Identity.WebApi
```

Then verify the configured base URL, Swagger/OpenAPI endpoint, and OIDC discovery document at:

```text
https://localhost:<identity-port>/.well-known/openid-configuration
```

Never commit local secrets, development database credentials, or certificate private keys.

## Testing Checklist

### Authentication and OIDC

- [ ] Register rejects duplicate email/username and weak passwords.
- [ ] Confirmation tokens are URL-safe, expire, and cannot be reused unexpectedly.
- [ ] Authorization Code + PKCE succeeds for a seeded client and rejects invalid redirect URIs/verifiers.
- [ ] Access and refresh token lifetimes match configuration.
- [ ] Identity REST endpoints reject cookie-only authentication and accept the required bearer policy.
- [ ] Other microservices validate issuer, signature, lifetime, and required claims.
- [ ] Forgot/reset-password responses do not enable account enumeration.

### Onboarding and authorization

- [ ] Only `PendingTenant` users can start onboarding.
- [ ] Successful onboarding sets `TenantId`, `UserType=Tenant`, and `TenantOwner`.
- [ ] Failed/retried Tenant Billing calls do not create duplicate or inconsistent tenants.
- [ ] Tenant managers cannot create or manage users in another tenant.
- [ ] Tenant admins cannot grant forbidden roles.
- [ ] Platform users never receive a tenant ID.
- [ ] Only `SuperAdmin` can create or manage another `SuperAdmin`.

### Invitations

- [ ] Generated temporary passwords satisfy Identity policy and use a cryptographic RNG.
- [ ] Invitation state sets `MustChangePassword` and a UTC expiry.
- [ ] Expired temporary passwords are rejected.
- [ ] First-login change clears both invitation fields.
- [ ] Resend invalidates the previous temporary password and sends a new one.
- [ ] Resend is rejected after the password has already been changed.
- [ ] Resend authorization enforces user type, role hierarchy, and tenant boundary.
- [ ] Email failures are logged safely and leave a recoverable state.

### Concurrent login and logout

- [ ] A second login within 20 minutes of activity is rejected.
- [ ] A login after more than 20 minutes replaces the logical session.
- [ ] Inactivity by itself does not log out the existing user.
- [ ] Activity tracking updates at the chosen throttle interval.
- [ ] Logout clears server-side login state and the client removes tokens.
- [ ] Tests document that old JWTs remain valid until expiry unless session validation or introspection is enabled.

Prefer real EF Core, `UserManager`, `RoleManager`, and application services in integration tests, while faking external email/current-user dependencies. SQL Server LocalDB or Testcontainers gives the closest match to production. SQLite is useful for fast relational tests but does not verify SQL Server-specific types, migrations, filters, or query behavior.

## Security Notes

- Enforce HTTPS everywhere and never put tokens in URLs.
- Prefer Authorization Code + PKCE; do not use password or implicit grants.
- Keep access tokens short-lived and rotate refresh tokens according to policy.
- Store refresh/access tokens in a client mechanism appropriate to the frontend threat model; avoid exposing long-lived tokens to JavaScript when a secure HttpOnly approach is available.
- Use `HttpOnly`, `Secure`, `SameSite=Lax` (or a deliberately chosen stricter policy), and `__Host-` cookie requirements for Identity cookies.
- Replace development signing/encryption certificates before production and protect private keys in a certificate store or key-management service.
- Validate issuer, audience/resources, scopes, token lifetime, roles, `user_type`, and tenant context at service boundaries.
- Never accept `tenant_id`, platform status, roles, or invitation state as trusted frontend assertions.
- Use generic public responses for login, forgot-password, and invitation lookup failures to reduce account enumeration.
- Do not log passwords, temporary passwords, access/refresh tokens, email tokens, client secrets, or full SendGrid error bodies containing sensitive data.
- HTML-encode user-controlled values included in email templates.
- Rate-limit login, forgot-password, resend-invitation, confirmation, and token endpoints; add lockout and monitoring for abuse.
- Use UTC for security timestamps and compare expiry values consistently.
- Treat email delivery as an external side effect; use idempotency and an outbox/queue when reliability requirements grow.
- Review data-protection key persistence if multiple Identity instances are deployed.
- Add audit events for privileged creation, role changes, invitation resend, login replacement, and logout.

## Architectural Summary

```text
Identity Service
├── Browser cookie session for OIDC interaction
├── OpenIddict server for signed token issuance
├── Local OpenIddict validation for its own bearer APIs
└── Identity/user/onboarding/invitation management

Other microservices
├── No Identity browser cookies
├── Bearer-token validation using Identity issuer/discovery
└── Their own tenant-scoped business authorization and data
```
