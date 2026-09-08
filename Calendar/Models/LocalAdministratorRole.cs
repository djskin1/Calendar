using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.Models
{
    public class LocalAdministratorRole
    {
        public int LocalAdministratorId { get; set; }
        public LocalAdministrator? LocalAdministrator { get; set; }

        public int RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
