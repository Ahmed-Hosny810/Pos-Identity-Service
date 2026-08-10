using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.PlatformAdmins.DTOS
{
    public class CreatePlatformAdminResult
    {
        public string Email { get; set; } = null!;

        public string Role { get; set; } = null!;
    }
}
