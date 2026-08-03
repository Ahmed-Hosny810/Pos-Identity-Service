using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Services
{
    public interface IPlatformService
    {
        Task<Response<string>> AssignPlatformAdminRoleAsync(string userId);
        Task<Response<string>> AssignPlatformSuperAdminRoleAsync(string userId);
    }
}
