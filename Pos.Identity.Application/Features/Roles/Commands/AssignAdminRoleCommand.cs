using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Pos.Identity.Application.Features.Roles.Commands
{
    public class AssignAdminRoleCommand : IRequest<Response<string>>
    {
        public string UserId { get; set; }
    }

    public class AssignAdminRoleCommandHandler
        : IRequestHandler<AssignAdminRoleCommand, Response<string>>
    {
        private readonly IRoleService _roleService;

        public AssignAdminRoleCommandHandler(IRoleService roleService)
            => _roleService = roleService;

        public async Task<Response<string>> Handle(
            AssignAdminRoleCommand request,
            CancellationToken cancellationToken)
        {
            return await _roleService.AssignAdminRoleAsync(request.UserId);
        }
    }
}
