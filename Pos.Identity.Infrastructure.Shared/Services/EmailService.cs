using Pos.Identity.Application.Interfaces.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text;
using Pos.Identity.Infrastructure.Shared.Settings;

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

            await SendEmailAsync(toEmail, subject, body, cancellationToken);
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

            await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
            var to = new EmailAddress(toEmail);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: null,
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

                throw new Exception($"Email sending failed with status {response.StatusCode}: {errorBody}");
            }

            _logger.LogInformation("Email successfully sent to {Email}", toEmail);
        }
    }
}