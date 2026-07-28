using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.SocialLogin
{
    public class SocialLoginCommand : IRequest<Response<LoginResult>>
    {
        public string Provider { get; set; }
        public string ProviderKey { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
    }

    public class SocialLoginCommandHandler
        : IRequestHandler<SocialLoginCommand, Response<LoginResult>>
    {
        private readonly IUserAuthenticationService _authService;

        public SocialLoginCommandHandler(IUserAuthenticationService authService)
            => _authService = authService;

        public async Task<Response<LoginResult>> Handle(
            SocialLoginCommand request,
            CancellationToken cancellationToken)
        {
            return await _authService.SocialLoginAsync(
                request.Provider,
                request.ProviderKey,
                request.Email,
                request.FullName);
        }
    }
}
