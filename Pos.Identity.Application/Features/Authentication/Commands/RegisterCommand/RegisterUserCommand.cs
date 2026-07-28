using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Pos.Identity.Application.Features.Authentication.Commands.RegisterCommand
{
    public class RegisterUserCommand : IRequest<Response<string>>
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Response<string>>
    {
        private readonly IUserAuthenticationService _authService;

        public RegisterUserCommandHandler(IUserAuthenticationService authService)
        {
            _authService = authService;
        }

        public async Task<Response<string>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
           var authResult=await _authService.RegisterAsync(request.UserName, request.Email, request.Password, request.FullName);
            if (!authResult.Succeeded)
            {
                throw new ApiException(string.Join(", ", authResult.Errors));
            }
            return new Response<string>(data:authResult.Data);
            
        }
    }
}