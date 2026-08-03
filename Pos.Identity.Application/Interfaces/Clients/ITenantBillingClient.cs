using Pos.Identity.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Clients
{
    public interface ITenantBillingClient
    {
        Task<CreateTenantResult> CreateTenantAsync(
            CreateTenantRequest request,
            CancellationToken cancellationToken);
    }
}
