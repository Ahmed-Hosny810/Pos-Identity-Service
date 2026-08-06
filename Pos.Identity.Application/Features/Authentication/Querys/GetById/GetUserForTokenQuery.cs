using MediatR;
using Microsoft.AspNetCore.Identity;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Features.Authentication.DTOS;
using Pos.Identity.Application.Wrappers;
using Pos.Identity.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.Querys.GetById
{
    public class GetUserForTokenQuery : IRequest<Response<UserForTokenResult>>
    {
        public string UserId { get; set; }
        public GetUserForTokenQuery(string userId) => UserId = userId;
    }
    public class GetUserForTokenQueryHandler:IRequestHandler<GetUserForTokenQuery, Response<UserForTokenResult>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetUserForTokenQueryHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        } 

        public async Task<Response<UserForTokenResult>> Handle(GetUserForTokenQuery request,CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null)
                throw new ApiException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            return new Response<UserForTokenResult>(new UserForTokenResult
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                UserType = user.UserType,
                TenantId = user.TenantId,
                IsActive = user.IsActive,
                Roles = roles
            });
        }
    }
}
