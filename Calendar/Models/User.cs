using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.Models
{
    public class User
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = "";

        public string? Email { get; set; }

        // Filled when the user comes from Microsoft Entra ID.
        public string? EntraObjectId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
