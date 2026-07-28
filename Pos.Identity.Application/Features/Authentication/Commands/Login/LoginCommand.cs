using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.Login
{
    public class LoginCommand : IRequest<Response<LoginResult>>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Response<LoginResult>>
    {
        private readonly IUserAuthenticationService _authService;

        public LoginCommandHandler(IUserAuthenticationService authService)
        {
            _authService = authService;
        }
        public async Task<Response<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            return await _authService.LoginAsync(request.Email, request.Password);
            
        }
    }
}
