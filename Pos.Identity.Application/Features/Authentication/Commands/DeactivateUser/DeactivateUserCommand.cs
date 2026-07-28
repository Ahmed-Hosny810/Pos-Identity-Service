using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.DeactivateUser
{
    public class DeactivateUserCommand : IRequest<Response<string>>
    {
        public string UserId { get; set; }
    }

    public class DeactivateUserCommandHandler
        : IRequestHandler<DeactivateUserCommand, Response<string>>
    {
        private readonly IUserAuthenticationService _authService;

        public DeactivateUserCommandHandler(IUserAuthenticationService authService)
            => _authService = authService;

        public async Task<Response<string>> Handle(
            DeactivateUserCommand request,
            CancellationToken cancellationToken)
        {
            return await _authService.DeactivateUserAsync(request.UserId);
        }
            
    }
}