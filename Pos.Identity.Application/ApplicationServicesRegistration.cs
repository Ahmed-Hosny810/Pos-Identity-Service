using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pos.Identity.Application.Behaviours;
using System.Reflection;
using System.Text;

namespace Pos.Identity.Application
{
    public static class ApplicationServicesRegistration
    {
        public static void AddApplicationLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
                cfg.AddMaps(Assembly.GetExecutingAssembly())
            );
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }
    }
}
