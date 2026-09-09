using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.Models
{
    public class EntraConfiguration
    {
        public int Id { get; set; }

        public bool IsEnabled { get; set; }

        public string TenantId { get; set; } = "";
        public string ClientId { get; set; } = "";

        public DateTime? LastGroupSyncAt { get; set; }

        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
