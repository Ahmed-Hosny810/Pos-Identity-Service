using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Infrastructure.Persistence.Constants
{
    public static class AuthenticationSchemes
    {
        public static readonly string ApplicationCookie = IdentityConstants.ApplicationScheme;

        public const string ExternalCookie =
            "ExternalCookie";

        public const string Google =
            "Google";

        public const string Facebook =
            "Facebook";
    }
}
