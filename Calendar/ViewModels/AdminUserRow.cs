using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.ViewModels
{
    public class AdminUserRow
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = "";
        public string? Email { get; set; }
        public bool IsActive { get; set; }

        public string Roles { get; set; } = "";
    }
}
