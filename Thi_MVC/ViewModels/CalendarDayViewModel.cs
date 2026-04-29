using CalendarAppointmentApp.Models;

namespace CalendarAppointmentApp.ViewModels;

public class CalendarDayViewModel
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
}