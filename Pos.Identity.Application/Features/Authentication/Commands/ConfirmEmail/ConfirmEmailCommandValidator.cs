using FluentValidation;
using Pos.Identity.Application.Features.Authentication.Commands.ConfirmEmail;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.ConfirmEmail
{
    public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Confirmation token is required");
        }
    }
}
