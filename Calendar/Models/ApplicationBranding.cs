namespace Calendar.Models
{
    public class ApplicationBranding
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = "Central Calendar";

        public byte[]? LogoData { get; set; }

        public string? LogoFileName { get; set; }

        public string? LogoContentType { get; set; }

        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
