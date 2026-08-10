using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Wrappers
{
    public class LoginResult
    {
        public string UserId { get; set; }= null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public Guid? TenantId { get; set; }
        public string UserType { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
        public bool MustChangePassword { get; set; }
        public bool IsLoggedIn { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }
}
