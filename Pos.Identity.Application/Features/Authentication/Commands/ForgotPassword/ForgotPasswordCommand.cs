using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.ForgotPassword
{
    public class ForgotPasswordCommand : IRequest<Response<string>>
    {
        public string Email { get; set; }
    }

    public class ForgotPasswordCommandHandler
        : IRequestHandler<ForgotPasswordCommand, Response<string>>
    {
        private readonly IUserAuthenticationService _authService;

        public ForgotPasswordCommandHandler(IUserAuthenticationService authService)
            => _authService = authService;

        public async Task<Response<string>> Handle(
            ForgotPasswordCommand request,
            CancellationToken cancellationToken)
        {
            return await _authService.ForgotPasswordAsync(request.Email);
        }
            
    }
}
