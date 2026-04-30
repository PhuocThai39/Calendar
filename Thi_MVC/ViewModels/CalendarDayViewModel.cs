namespace CalendarAppointmentApp.ViewModels
{
    public class CalendarDayViewModel
    {
        public DateTime Date { get; set; }

        // Giúp View làm mờ các ngày của tháng trước/tháng sau
        public bool IsCurrentMonth { get; set; }

        // Thêm thuộc tính này để View có thể highlight (tô màu khác) cho "Ngày hôm nay"
        public bool IsToday => Date.Date == DateTime.Today;

        // ĐÃ ĐIỀU CHỈNH: Đổi từ Appointment sang AppointmentListViewModel để chứa được cả Lịch nhóm
        public List<AppointmentListViewModel> Appointments { get; set; } = new();

        // Tiện ích: Kiểm tra nhanh xem ngày này có lịch hay không để hiển thị icon/chấm nhỏ
        public bool HasAppointments => Appointments.Any();
    }
}