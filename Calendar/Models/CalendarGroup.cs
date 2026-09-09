using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.Models
{
    public class CalendarGroup
    {
        public int Id { get; set; }

        public string EntraObjectId { get; set; } = "";

        public string DisplayName { get; set; } = "";
        public string? Description { get; set; }

        public bool IsSecurityGroup { get; set; }
        public bool IsMicrosoft365Group { get; set; }

        public bool IsVisibleInCalendar { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime? LastSyncedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
