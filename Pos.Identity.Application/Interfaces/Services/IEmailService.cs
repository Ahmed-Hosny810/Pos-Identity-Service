using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string toEmail, string userId, string token, CancellationToken cancellationToken = default);
        Task SendPasswordResetEmailAsync(string toEmail, string userId, string token, CancellationToken cancellationToken = default);
        Task SendTenantUserInvitationEmailAsync(string email,string fullName,string temporaryPassword,DateTime temporaryPasswordExpiresAt,CancellationToken cancellationToken = default);
        Task SendPlatformAdminInvitationEmailAsync(string email,string fullName,string temporaryPassword,DateTime temporaryPasswordExpiresAt,CancellationToken cancellationToken = default);
        Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string body, CancellationToken cancellationToken = default);
    }
}
