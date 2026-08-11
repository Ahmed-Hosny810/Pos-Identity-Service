using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangeTempPasswordCommandValidator:AbstractValidator<ChangeTempPasswordCommand>
    {
        public ChangeTempPasswordCommandValidator()
        {
            RuleFor(x => x.TempPassword)
                .NotEmpty().WithMessage("Temporary password is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }
}
