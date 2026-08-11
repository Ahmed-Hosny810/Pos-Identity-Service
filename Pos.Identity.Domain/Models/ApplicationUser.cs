using Microsoft.AspNetCore.Identity;
using Pos.Identity.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Domain.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string FullName { get; set; }

        public Guid? TenantId { get; set; }

        public string UserType { get; set; } = null!;
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        
        public bool MustChangePassword { get; set; }

        public DateTime? TemporaryPasswordExpiresAt { get; set; }

        public bool IsLoggedIn { get; set; }

        public DateTime? LastAccessedAt { get; set; }

        public bool IsTenantUser => UserType == UserTypes.Tenant;

        public bool IsPlatformUser => UserType == UserTypes.Platform;

        public void Activate()
        {
            IsActive = true;
        }

        public void Disable()
        {
            IsActive = false;
        }
    }
}
