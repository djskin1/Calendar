
namespace Calendar.Models
{
    public class SystemInformation
    {
        public int Id { get; set; }

        public string LatestClientVersion { get; set; } = "2.0.0";

        public string MinimumClientVersion { get; set; } = "2.0.0";

        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
