namespace Calendar.ViewModels
{
    public class CalendarStatusOption
    {
        public int Id { get; set; }

        public string Code { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public Calendar.Models.CalendarStatus Status { get; set; } = null!;
    }
}