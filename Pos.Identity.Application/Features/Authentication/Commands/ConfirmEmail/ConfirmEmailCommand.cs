using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Pos.Identity.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.ConfirmEmail
{
    public class ConfirmEmailCommand : IRequest<Response<bool>>
    {
        public string UserId { get; set; }
        public string Token { get; set; }
    }

    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Response<bool>>
    {
        private readonly IUserAuthenticationService _authService;

        public ConfirmEmailCommandHandler(IUserAuthenticationService authService)
        {
            _authService = authService;
        }

        public async Task<Response<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
           var result = await _authService.ConfirmEmailAsync(request.UserId, request.Token);
            if (!result.Succeeded)
            {
                throw new ApiException(string.Join(", ", result.Errors));
            }
            return new Response<bool>(data: result.Data, message: "Email confirmed successfully.");

        }
    }
}
