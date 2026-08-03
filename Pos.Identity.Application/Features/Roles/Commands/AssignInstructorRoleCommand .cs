using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Pos.Identity.Application.Features.Roles.Commands
{
    public class AssignInstructorRoleCommand : IRequest<Response<string>>
    {
        public string UserId { get; set; }
    }

    public class AssignInstructorRoleCommandHandler
        : IRequestHandler<AssignInstructorRoleCommand, Response<string>>
    {
        private readonly IPlatformService _roleService;

        public AssignInstructorRoleCommandHandler(IPlatformService roleService)
            => _roleService = roleService;

        public async Task<Response<string>> Handle(
            AssignInstructorRoleCommand request,
            CancellationToken cancellationToken)
        {
            return await _roleService.AssignInstructorRoleAsync(request.UserId);
        }
    }
}
