using Calendar.Models;

public class CalendarEntry
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime Date { get; set; }

    public string StatusCode { get; set; } = "";

    public string? Notes { get; set; }

    public string Recurrence { get; set; } = "Once";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    public User? User { get; set; }
}