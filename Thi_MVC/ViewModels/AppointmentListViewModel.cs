namespace CalendarAppointmentApp.ViewModels
{
    // ĐÃ THÊM: Class nhỏ dùng để chứa thông tin thành viên nhóm
    public class ParticipantInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class AppointmentListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public bool IsGroupMeeting { get; set; }
        public int DurationInMinutes => (int)(EndTime - StartTime).TotalMinutes;

        public List<string>? ReminderMessages { get; set; }

        // ĐÃ THÊM: Danh sách lưu trữ các thành viên tham gia
        public List<ParticipantInfo> Participants { get; set; } = new List<ParticipantInfo>();
    }
}