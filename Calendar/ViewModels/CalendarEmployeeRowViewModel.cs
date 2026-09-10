using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.ViewModels
{
    public class CalendarEmployeeRowViewModel
    {
        public int UserID { get; set; }
        public string DisplayName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public List<CalendarDayCellViewModel> Days { get; set; } = new();
    }
}
