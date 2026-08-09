using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Pos.Auth.WebApi.Services;
using Pos.Identity.Application;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Infrastructure.Persistence;
using Pos.Identity.Infrastructure.Persistence.Context;
using Pos.Identity.Infrastructure.Shared;
using Pos.Identity.Infrastructure.Shared.Seeders;
using Pos.Identity.WebApi.Extensions;
using Pos.Identity.WebApi.Middlewares;
using Serilog;

namespace Pos.Auth.WebApi
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                 .WriteTo.Console()
                 .CreateBootstrapLogger();

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            });

            builder.Services.AddControllers();

            // AddHealthChecks
            builder.Services
                     .AddHealthChecks()
                     .AddDbContextCheck<ApplicationDbContext>(
                         name: "tenant-billing-db",
                         failureStatus: HealthStatus.Unhealthy,
                         tags: new[] { "ready", "db" });


            // API Versioning
            builder.Services.AddApiVersioningExtension();
            // AddPersistenceServices
            builder.Services.AddPersistenceServices(builder.Configuration);
            builder.Services.AddSharedInfrastructureServices(builder.Configuration);
            builder.Services.AddApplicationLayer();
            builder.Services.AddOpenIddictServer(builder.Configuration);
            builder.Services.AddSocialAuthentication(builder.Configuration);

            // Swagger (via extension)
            builder.Services.AddSwaggerExtension();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            var app = builder.Build();

            app.UseMiddleware<ErrorHandlerMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerExtension();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            //app process is alive.
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            //the app is ready to receive traffic, including database check
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });

            app.MapControllers();
            // app.MapRazorPages();

            app.Run();
        }
    }
}
