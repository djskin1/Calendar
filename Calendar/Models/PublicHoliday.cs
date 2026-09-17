namespace Calendar.Models
{
    public class PublicHoliday
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public DateTime Date { get; set; }
        public string CountryCode { get; set; } = "";
        public bool IsNationalHoliday { get; set; }
        public bool IsOfficialPublicHoliday { get; set; }

        public string Source { get; set; } = "Api";

        public bool IsActive { get; set; } = true;

        public DateTime? SyncedAt { get; set; }
    }
}