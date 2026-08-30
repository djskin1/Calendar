namespace Calendar.Models
{
    public class SearchResultItem
    {
        public string Type { get; set; } = "";

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string DateText { get; set; } = "";

        public string Icon { get; set; } = "";

        public DateTime? Date { get; set; }

        public int? UserId { get; set; }
    }
}