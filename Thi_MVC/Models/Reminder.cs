using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalendarAppointmentApp.Models;

public class Reminder
{
    [Key]
    public int Id { get; set; }

    // Thay vì dùng DateTime Time, ta dùng số phút. Ví dụ: 15 (nhắc trước 15 phút)
    [Required(ErrorMessage = "Vui lòng chọn thời gian nhắc trước")]
    [Range(0, 43200, ErrorMessage = "Thời gian nhắc trước phải từ 0 đến 43200 phút (30 ngày)")]
    public int MinutesBefore { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung nhắc nhở")]
    [StringLength(300, ErrorMessage = "Nội dung nhắc nhở không được vượt quá 300 ký tự")]
    public string Message { get; set; } = string.Empty;

    // Khóa ngoại
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    // (Tùy chọn) Hàm hỗ trợ tính toán ra giờ báo thức chính xác lúc runtime
    // Dùng [NotMapped] để EF Core không tạo cột này dưới database
    [NotMapped]
    public DateTime? TriggerTime
    {
        get
        {
            // Nếu đã nạp (include) thông tin Appointment, ta tính ra giờ báo thức
            if (Appointment != null)
            {
                return Appointment.StartTime.AddMinutes(-MinutesBefore);
            }
            return null;
        }
    }
}