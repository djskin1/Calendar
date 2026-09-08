using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.ViewModels
{
    public class AdminLocalAdministratorRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsEnabled { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string Roles { get; set; } = "";
    }
}
