using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Features.Authentication.DTOS
{
    public class UserForTokenResult
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string UserType { get; set; }
        public Guid? TenantId { get; set; }
        public bool IsActive { get; set; }
        public IList<string> Roles { get; set; }
    }
}
