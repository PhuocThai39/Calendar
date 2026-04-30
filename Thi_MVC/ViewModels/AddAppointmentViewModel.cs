using System.ComponentModel.DataAnnotations;

namespace CalendarAppointmentApp.ViewModels;

public class AddAppointmentViewModel
{
    [Required(ErrorMessage = "Tên lịch hẹn không được để trống")]
    [StringLength(150, ErrorMessage = "Tên không được quá 150 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa điểm không được để trống")]
    [StringLength(200, ErrorMessage = "Địa điểm không được quá 200 ký tự")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
    public DateTime EndTime { get; set; }

    // --- Phần Nhắc nhở (Reminder) ---
    public bool HasReminder { get; set; }

    // ĐÃ ĐIỀU CHỈNH: Đổi từ DateTime? sang int? để lưu số phút nhắc trước (VD: 15, 30, 60)
    [Range(0, 43200, ErrorMessage = "Thời gian nhắc trước không hợp lệ")]
    public int? ReminderMinutesBefore { get; set; }

    public string? ReminderMessage { get; set; }

    // --- Các cờ điều khiển logic (Smart Flags) ---

    // 1. ĐÃ THÊM: Biến này dùng để nhận giá trị từ Radio Button (Chọn "Lịch cá nhân" hay "Bắt đầu nhóm mới")
    public bool IsStartNewGroupMeeting { get; set; }

    // Dùng để xác nhận có ghi đè lịch cũ khi bị trùng không
    public bool ReplaceOldAppointment { get; set; }

    // 2. ĐÃ SỬA: Chuyển thành bool? (có thể null). 
    // Nếu null: Người dùng chưa được hỏi. Nếu true: Đồng ý tham gia. Nếu false: Từ chối, vẫn tạo lịch cá nhân.
    public bool? JoinGroupMeeting { get; set; }

    // Lưu ID của lịch hẹn bị trùng hoặc cuộc họp nhóm tương ứng để Controller xử lý
    public int? ConflictAppointmentId { get; set; }
    public int? MatchingGroupMeetingId { get; set; }

    // 3. ĐÃ THÊM: Lưu tên của nhóm đang bị trùng để hiển thị câu hỏi (VD: "Có nhóm [Tên nhóm] đang họp, bạn có muốn vào không?")
    public string? ConflictGroupMeetingName { get; set; }

    public bool IsGroupEdit { get; set; }
}