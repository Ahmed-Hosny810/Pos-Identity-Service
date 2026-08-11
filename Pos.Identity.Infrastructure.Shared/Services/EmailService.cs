using Pos.Identity.Application.Interfaces.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text;
using Pos.Identity.Infrastructure.Shared.Settings;
using Pos.Identity.Application.Exceptions;

namespace Pos.Identity.Infrastructure.Shared.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ISendGridClient _sendGridClient;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ISendGridClient sendGridClient,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _sendGridClient = sendGridClient;
            _logger = logger;
        }

        public async Task SendConfirmationEmailAsync(
            string toEmail,
            string userId,
            string token,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending confirmation email requested for Email: {Email} .UserId: {userId} ",toEmail,userId);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmationLink = $"{_emailSettings.AppBaseUrl}/api/{_emailSettings.ApiVersion}/Authentication/ConfirmEmail?UserId={userId}&Token={encodedToken}";
            var expiryHours = _emailSettings.ConfirmationTokenExpiryHours;

            var subject = "Confirm Your Email Address";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>Confirm Your Email Address</h2>
                    <p>Thank you for registering. Please confirm your email address by clicking the button below:</p>
                    <a href='{confirmationLink}' 
                       style='display: inline-block; padding: 12px 24px; background-color: #4F46E5;
                              color: white; text-decoration: none; border-radius: 4px; margin: 16px 0;'>
                        Confirm Email
                    </a>
                    <p>Or copy and paste this link into your browser:</p>
                    <p style='color: #6B7280; word-break: break-all;'>{confirmationLink}</p>
                    <p>This link will expire in {expiryHours} hours.</p>
                    <p>If you did not create an account, please ignore this email.</p>
                </body>
                </html>";

            var plainTextContent = $@"
            Thank you for registering.
            
            Please confirm your email address using this link:
            {confirmationLink}
            
            This link will expire in {expiryHours} hours.
            
            If you did not create an account, please ignore this email.";

            await SendEmailAsync(toEmail, subject, plainTextContent, body, cancellationToken);
        }

        public async Task SendPasswordResetEmailAsync(
            string toEmail,
            string userId,
            string token,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending Password Reset email requested for Email: {Email} .UserId: {userId}", toEmail, userId);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = $"{_emailSettings.AppBaseUrl}/api/{_emailSettings.ApiVersion}/Authentication/reset-password?UserId={userId}&Token={encodedToken}";
            var expiryHours = _emailSettings.PasswordResetTokenExpiryHours;

            var subject = "Reset Your Password";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>Reset Your Password</h2>
                    <p>You requested a password reset. Click the button below to set a new password:</p>
                    <a href='{resetLink}' 
                       style='display: inline-block; padding: 12px 24px; background-color: #4F46E5;
                              color: white; text-decoration: none; border-radius: 4px; margin: 16px 0;'>
                        Reset Password
                    </a>
                    <p>Or copy and paste this link into your browser:</p>
                    <p style='color: #6B7280; word-break: break-all;'>{resetLink}</p>
                    <p>This link will expire in {expiryHours} hour(s).</p>
                    <p>If you did not request a password reset, please ignore this email.</p>
                </body>
                </html>";

            var plainTextContent = $@"
            You requested a password reset.
            
            Use this link to set a new password:
            {resetLink}
            
            This link will expire in {expiryHours} hour(s).
            
            If you did not request a password reset, please ignore this email.";

            await SendEmailAsync(
                toEmail,
                subject,
                plainTextContent,
                body,
                cancellationToken);
        }

        public async Task SendTenantUserInvitationEmailAsync(string email, string fullName, string temporaryPassword, DateTime temporaryPasswordExpiresAt, CancellationToken cancellationToken = default)
        {
            var subject = "You have been invited to Vendora POS";

            var expiresAt = temporaryPasswordExpiresAt.ToString("yyyy-MM-dd HH:mm 'UTC'");

            var htmlContent = $@"
             <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; color: #222;'>
                 <h2 style='color: #111;'>Welcome to Vendora POS</h2>

                 <p>Hello {fullName},</p>

                 <p>
                     You have been invited to join your company workspace on 
                     <strong>Vendora POS</strong>.
                 </p>

                 <p>
                     Use the temporary password below to login:
                 </p>

                 <div style='background: #f4f4f4; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                     <p style='margin: 0; font-size: 14px; color: #555;'>Temporary Password</p>
                     <p style='margin: 8px 0 0; font-size: 22px; font-weight: bold; letter-spacing: 1px;'>
                         {temporaryPassword}
                     </p>
                 </div>

                 <p>
                     This temporary password will expire on:
                     <strong>{expiresAt}</strong>
                 </p>

                 <p>
                     After logging in, you will be asked to change your password before using the system.
                 </p>

                 <p style='margin-top: 32px; color: #666; font-size: 13px;'>
                     If you were not expecting this invitation, please ignore this email.
                 </p>
             </div>";

            var plainTextContent = $@"
            Hello {fullName},
            
            You have been invited to join your company workspace on Vendora POS.
            
            Temporary password:
            {temporaryPassword}
            
            This temporary password expires on: {expiresAt}
            
            After logging in, you will be asked to change your password.
            
            If you were not expecting this invitation, please ignore this email.";


            await SendEmailAsync(
                email,
                subject,
                plainTextContent,
                htmlContent,
                cancellationToken);
        }


        public async Task SendPlatformAdminInvitationEmailAsync( string email,string fullName,string temporaryPassword,DateTime temporaryPasswordExpiresAt,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Sending platform admin invitation email requested for Email: {Email}",
                email);

            var subject = "You have been invited as a Vendora Platform Admin";

            var expiresAt = temporaryPasswordExpiresAt.ToString("yyyy-MM-dd HH:mm 'UTC'");

            var htmlContent = $@"
             <html>
             <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; color: #222;'>
                 <h2 style='color: #111;'>Welcome to Vendora Admin Portal</h2>

                 <p>Hello {fullName},</p>

                 <p>
                     You have been invited to join the 
                     <strong>Vendora Platform Admin Portal</strong>.
                 </p>

                 <p>
                     Use the temporary password below to login:
                 </p>

                 <div style='background: #f4f4f4; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                     <p style='margin: 0; font-size: 14px; color: #555;'>Temporary Password</p>
                     <p style='margin: 8px 0 0; font-size: 22px; font-weight: bold; letter-spacing: 1px;'>
                         {temporaryPassword}
                     </p>
                 </div>

                 <p>
                     This temporary password will expire on:
                     <strong>{expiresAt}</strong>
                 </p>

                 <p>
                     After logging in, you will be asked to change your password before using the system.
                 </p>

                 <p style='margin-top: 32px; color: #666; font-size: 13px;'>
                     If you were not expecting this invitation, please ignore this email.
                 </p>
             </body>
             </html>";

            var plainTextContent = $@"
             Hello {fullName},
             
             You have been invited to join the Vendora Platform Admin Portal.
             
             Temporary password:
             {temporaryPassword}
             
             This temporary password expires on: {expiresAt}
             
             After logging in, you will be asked to change your password.
             
             If you were not expecting this invitation, please ignore this email.";

            await SendEmailAsync(
                email,
                subject,
                plainTextContent,
                htmlContent,
                cancellationToken);
        }
        public async Task ResendUserInvitationEmailAsync(string email, string fullName, string temporaryPassword, DateTime temporaryPasswordExpiresAt, string portalName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
            "Resending user invitation email requested for Email: {Email}, Portal: {PortalName}",
            email,
            portalName);

            var subject = $"Your {portalName} invitation has been resent";

            var expiresAt = temporaryPasswordExpiresAt.ToString("yyyy-MM-dd HH:mm 'UTC'");

            var htmlContent = $@"
             <html>
             <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; color: #222;'>
                 <h2 style='color: #111;'>Your invitation has been resent</h2>

                 <p>Hello {fullName},</p>

                 <p>
                     Your invitation to access 
                     <strong>{portalName}</strong> has been resent.
                 </p>

                 <p>
                     Please use the new temporary password below to login:
                 </p>

                 <div style='background: #f4f4f4; padding: 16px; border-radius: 8px; margin: 20px 0;'>
                     <p style='margin: 0; font-size: 14px; color: #555;'>Temporary Password</p>
                     <p style='margin: 8px 0 0; font-size: 22px; font-weight: bold; letter-spacing: 1px;'>
                         {temporaryPassword}
                     </p>
                 </div>

                 <p>
                     This temporary password will expire on:
                     <strong>{expiresAt}</strong>
                 </p>

                 <p>
                     After logging in, you will be asked to change your password before using the system.
                 </p>

                 <p style='margin-top: 32px; color: #666; font-size: 13px;'>
                     If you were not expecting this invitation, please ignore this email.
                 </p>
             </body>
             </html>";

            var plainTextContent = $@"
            Hello {fullName},
            
            Your invitation to access {portalName} has been resent.
            
            Please use the new temporary password below to login:
            
            Temporary password:
            {temporaryPassword}
            
            This temporary password expires on: {expiresAt}
            
            After logging in, you will be asked to change your password before using the system.
            
            If you were not expecting this invitation, please ignore this email.";

            await SendEmailAsync(
                email,
                subject,
                plainTextContent,
                htmlContent,
                cancellationToken);
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string painTextContent,
            string body,
            CancellationToken cancellationToken = default)
        {
            var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
            var to = new EmailAddress(toEmail);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: painTextContent,
                htmlContent: body
            );

            _logger.LogInformation("Sending email to {Email} with subject '{Subject}'", toEmail, subject);

            var response = await _sendGridClient.SendEmailAsync(message, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Email sending failed for {Email}. Status: {StatusCode}. Error: {Error}",
                    toEmail, response.StatusCode, errorBody);

                throw new ApiException($"Email sending failed with status {response.StatusCode}: {errorBody}");
            }

            _logger.LogInformation("Email successfully sent to {Email}", toEmail);
        }
    }
}