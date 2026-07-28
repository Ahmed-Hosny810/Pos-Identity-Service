using Pos.Identity.Application.Features.Authentication.DTOS;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Querys.GetUserStatus
{
    public class GetUserStatusQuery : IRequest<Response<UserStatusResult>>
    {
        public string UserId { get; set; }
    }
    public class GetUserStatusQueryHandler : IRequestHandler<GetUserStatusQuery, Response<UserStatusResult>>
    {
        private readonly IUserAuthenticationService _authenticationService;

        public GetUserStatusQueryHandler(IUserAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }
        public async Task<Response<UserStatusResult>> Handle(GetUserStatusQuery request, CancellationToken cancellationToken)
        {
            var result = await _authenticationService.GetUserStatus(request.UserId);

            return new Response<UserStatusResult>(new UserStatusResult { IsActive = result });
        }
    }
}