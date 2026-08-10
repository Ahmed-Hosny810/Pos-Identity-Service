using FluentValidation;
using Pos.Identity.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.PlatformAdmins.Commands
{
    public class CreatePlatformAdminCommandValidator
       : AbstractValidator<CreatePlatformAdminCommand>
    {
        public CreatePlatformAdminCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.UserName)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(150);

            RuleFor(x => x.Role)
                .NotEmpty()
                .Must(BeAllowedPlatformRole)
                .WithMessage("Invalid platform admin role.");
        }

        private static bool BeAllowedPlatformRole(string role)
        {
            var allowedRoles = new[]
            {
                PlatformRoles.SuperAdmin,
                PlatformRoles.Admin
            };

            return allowedRoles.Contains(role);
        }
    }
}
