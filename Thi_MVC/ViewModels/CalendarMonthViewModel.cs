namespace CalendarAppointmentApp.ViewModels;

public class CalendarMonthViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }

    // Thêm tiện ích: Trả về chuỗi tiêu đề thân thiện để in ra màn hình (VD: "Tháng 4, 2026")
    public string Title => $"Tháng {Month}, {Year}";

    public DateTime PreviousMonth { get; set; }
    public DateTime NextMonth { get; set; }

    // Danh sách 42 ngày (6 tuần) để vẽ lưới Calendar
    public List<CalendarDayViewModel> Days { get; set; } = new();
}