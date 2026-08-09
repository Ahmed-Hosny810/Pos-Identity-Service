using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.TenantUsers.DTOS
{
    public class CreateTenantUserResult
    {
        public string UserId { get; set; } = null!;
        public Guid TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
}
