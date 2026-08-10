using FluentValidation;
using Pos.Identity.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.TenantUsers.Commands.CreateCommand
{
    public class CreateTenantUserCommandValidator
       : AbstractValidator<CreateTenantUserCommand>
    {
        public CreateTenantUserCommandValidator()
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
                .Must(BeAllowedTenantRole)
                .WithMessage("Invalid tenant user role.");
        }

        private static bool BeAllowedTenantRole(string role)
        {
            var allowedRoles = new[]
            {
                TenantRoles.Admin,
                TenantRoles.Cashier,
                TenantRoles.InventoryStaff
            };

            return allowedRoles.Contains(role);
        }
    }
}
