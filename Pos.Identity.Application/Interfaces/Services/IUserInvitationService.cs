using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Services
{
    public interface IUserInvitationService
    {
        Task ResendInvitationAsync(string userId, CancellationToken cancellationToken);
    }
}
