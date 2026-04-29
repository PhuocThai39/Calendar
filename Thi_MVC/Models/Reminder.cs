using System.ComponentModel.DataAnnotations;

namespace CalendarAppointmentApp.Models;

public class Reminder
{
    public int Id { get; set; }

    public DateTime Time { get; set; }

    [Required, StringLength(300)]
    public string Message { get; set; } = string.Empty;

    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
}