namespace CalendarAppointmentApp.Models;

public class Calendar
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();

    public bool CheckConflict(DateTime start, DateTime end)
    {
        return Appointments.Any(a =>
            a.StartTime < end && start < a.EndTime
        );
    }
}