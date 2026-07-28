using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<Response<string>> AssignAdminRoleAsync(string userId);
        Task<Response<string>> AssignInstructorRoleAsync(string userId);
    }
}
