using Pos.Identity.Application.Features.TenantUsers.Commands.CreateCommand;
using Pos.Identity.Application.Features.TenantUsers.DTOS;
using Pos.Identity.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Services
{
    public class TenantUserService : ITenantUserService
    {
        public Task<CreateTenantUserResult> CreateTenantUserAsync(CreateTenantUserCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
