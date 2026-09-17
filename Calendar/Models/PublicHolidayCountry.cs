using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.Models
{
    public class PublicHolidayCountry
    {
        public int Id { get; set; }
        public string CountryCode { get; set; } = "";
        public string CountryName { get; set; } = "";

        public bool IsSelected { get; set; }

        public bool IsBlockingCountry { get; set; }

        public int? LastSyncedYear { get; set; }

        public DateTime? LastSyncedAt { get; set; }
    }
}
