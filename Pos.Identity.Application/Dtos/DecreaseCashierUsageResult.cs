using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Dtos
{
    public class DecreaseCashierUsageResult
    {
        public Guid TenantId { get; set; }
        public int UsedCashiers { get; set; }
    }
}
