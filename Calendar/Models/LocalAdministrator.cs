namespace Calendar.Models
{
    public class LocalAdministrator
    {
        public int Id { get; set; }

        public string Username { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public string PasswordSalt { get; set; } = "";

        public int PasswordIterations { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }
    }
}