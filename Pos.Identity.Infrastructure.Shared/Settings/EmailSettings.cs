using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Settings
{
    public class EmailSettings
    {
        public string SendGridApiKey { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string AppBaseUrl { get; set; }
        public string ApiVersion { get; set; }

        public int ConfirmationTokenExpiryHours { get; set; } = 24;
        public int PasswordResetTokenExpiryHours { get; set; } = 1;
    }
}
