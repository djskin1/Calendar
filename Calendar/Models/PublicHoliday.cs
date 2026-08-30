namespace Calendar.Models
{
    public class PublicHoliday
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public DateTime Date { get; set; }

        public bool IsActive { get; set; } = true;
    }
}