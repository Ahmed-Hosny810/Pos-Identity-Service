using MediatR;
using Pos.Identity.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.Logout
{
    public class LogoutCommand:IRequest
    {
    }
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IUserAuthenticationService _userAuthenticationService;

        public LogoutCommandHandler(IUserAuthenticationService userAuthenticationService)
        {
            _userAuthenticationService = userAuthenticationService;
        }
        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
           await _userAuthenticationService.LogoutAsync();     
        }
    }
}
