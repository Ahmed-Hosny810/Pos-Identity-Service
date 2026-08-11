using MediatR;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangeTempPasswordCommand:IRequest<Response<string>>
    {
        public string TempPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
    public class ChangeTempPasswordCommandHandler : IRequestHandler<ChangeTempPasswordCommand, Response<string>>
    {
        private readonly IUserAuthenticationService _authService;
        public ChangeTempPasswordCommandHandler(IUserAuthenticationService authService)
        {
           _authService = authService;
        }

        public async Task<Response<string>> Handle(ChangeTempPasswordCommand request, CancellationToken cancellationToken)
        {
            return await _authService.ChangeTemporaryPasswordAsync(request.TempPassword, request.NewPassword);
        }
    }
}
