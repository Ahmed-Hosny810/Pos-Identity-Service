using Pos.Identity.Application.Features.PlatformAdmins.Commands;
using Pos.Identity.Application.Features.PlatformAdmins.DTOS;
using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Services
{
    public interface IPlatformAdminService
    {
        Task<CreatePlatformAdminResult> CreatePlatformAdminAsync(
            CreatePlatformAdminCommand request,
            CancellationToken cancellationToken);
    }
}
