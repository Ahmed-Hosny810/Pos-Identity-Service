using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Application.Dtos
{
    public class CreateTenantRequest
    {
        public string NameAr { get; set; } = null!;

        public string NameEn { get; set; } = null!;

        public string BusinessTypeCode { get; set; } = null!;

        public string CurrencyCode { get; set; } = "EGP";

        public string InventoryMode { get; set; } = "TrackStock";

        public string PlanCode { get; set; } = null!;
    }
}
