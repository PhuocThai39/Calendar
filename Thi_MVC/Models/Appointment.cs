using System.ComponentModel.DataAnnotations;

namespace CalendarAppointmentApp.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int CalendarId { get; set; }
    public Calendar? Calendar { get; set; }

    public ICollection<Reminder> Reminders { get; set; }
        = new List<Reminder>();

    public int GetDuration()
    {
        return (int)(EndTime - StartTime).TotalMinutes;
    }
}