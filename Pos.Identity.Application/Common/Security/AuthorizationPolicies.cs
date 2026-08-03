using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Common.Security
{
    public static class AuthorizationPolicies
    {
        public const string PlatformOnly = "PlatformOnly";
        public const string TenantOnly = "TenantOnly";
        public const string CanManageTenantUsers = "CanManageTenantUsers";
        public const string CanManageSubscription = "CanManageSubscription";
    }
}
