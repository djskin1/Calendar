using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.Models
{
    public class CalendarStatus
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? Description { get; set; }
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string ForegroundColor { get; set; } = "#000000";
        public bool IsActive { get; set; } = true;
        public bool IsSelectable { get; set; } = true;
        public bool IsSystemStatus { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt {  get; set; }

    }
}
