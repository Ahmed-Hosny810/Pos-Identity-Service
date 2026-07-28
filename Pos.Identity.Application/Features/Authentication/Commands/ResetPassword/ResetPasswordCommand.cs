using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommand : IRequest<Response<string>>
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }

    public class ResetPasswordCommandHandler
        : IRequestHandler<ResetPasswordCommand, Response<string>>
    {
        private readonly IUserAuthenticationService _authService;

        public ResetPasswordCommandHandler(IUserAuthenticationService authService)
            => _authService = authService;

        public async Task<Response<string>> Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            return await _authService.ResetPasswordAsync(
                request.UserId,
                request.Token,
                request.NewPassword);
        }
             
    }
}
