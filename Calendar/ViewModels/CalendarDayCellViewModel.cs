using System.Windows.Media;

namespace Calendar.ViewModels
{
    public class CalendarDayCellViewModel
    {
        public DateTime Date { get; set; }
        public string Text { get; set; } = "";
        public string? ToolTip { get; set; }
        public Brush Background { get; set; } = Brushes.Transparent;
        public Brush Foreground { get; set; } = Brushes.Black;
        public bool IsWeekend { get; set; }
        public bool IsPublicHoliday { get; set; }
        public int? CalendarEntryId { get; set; }
    }
}
