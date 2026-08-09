using MediatR;
using Pos.Identity.Application.Features.TenantUsers.DTOS;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.TenantUsers.Commands.CreateCommand
{
    public class CreateTenantUserCommand:IRequest<Response<CreateTenantUserResult>>
    {
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
    public class CreateTenantUserCommandHandler: IRequestHandler<CreateTenantUserCommand, Response<CreateTenantUserResult>>
    {
        private readonly ITenantUserService _tenantUserService;
        public CreateTenantUserCommandHandler(ITenantUserService tenantUserService)
        {
            _tenantUserService = tenantUserService;
        }
        public async Task<Response<CreateTenantUserResult>> Handle(CreateTenantUserCommand request, CancellationToken cancellationToken)
        {
            var result = await _tenantUserService.CreateTenantUserAsync(request, cancellationToken);
            return new Response<CreateTenantUserResult>(result);
        }
    }
}
