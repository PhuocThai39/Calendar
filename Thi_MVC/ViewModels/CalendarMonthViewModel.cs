namespace CalendarAppointmentApp.ViewModels;

public class CalendarMonthViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }

    public DateTime PreviousMonth { get; set; }
    public DateTime NextMonth { get; set; }

    public List<CalendarDayViewModel> Days { get; set; } = new();
}