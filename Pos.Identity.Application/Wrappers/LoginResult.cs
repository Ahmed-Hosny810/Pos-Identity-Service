using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Wrappers
{
    public class LoginResult
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public IList<string> Roles { get; set; }
    }
}
