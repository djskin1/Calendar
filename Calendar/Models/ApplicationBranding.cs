namespace Calendar.Models
{
    public class ApplicationBranding
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = "Central Calendar";

        public byte[]? LogoData { get; set; }

        public string? LogoFileName { get; set; }

        public string? LogoContentType { get; set; }

        public string PrimaryColor { get; set; } = "#0B856D";
        public string AccentColor { get; set; } = "#0097A7";

        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
