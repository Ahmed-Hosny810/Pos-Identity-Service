using Pos.Identity.Application.Features.TenantUsers.Commands.CreateCommand;
using Pos.Identity.Application.Features.TenantUsers.DTOS;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Services
{
    public interface ITenantUserService
    {
        Task<CreateTenantUserResult> CreateTenantUserAsync(
            CreateTenantUserCommand request,
            CancellationToken cancellationToken);
    }
}
