using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Thêm thư viện này để dùng [NotMapped]

namespace CalendarAppointmentApp.Models;

public class Appointment : IValidatableObject // Kế thừa interface để validate logic
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên/tiêu đề cuộc hẹn")]
    [StringLength(150, ErrorMessage = "Tên không được vượt quá 150 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa điểm")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
    public DateTime EndTime { get; set; }

    // Khóa ngoại liên kết với Calendar
    public int CalendarId { get; set; }
    public Calendar? Calendar { get; set; }

    public ICollection<Reminder> Reminders { get; set; }
        = new List<Reminder>();

    // Dùng [NotMapped] để EF Core bỏ qua, không tạo cột này trong Database
    [NotMapped]
    public int DurationInMinutes => (int)(EndTime - StartTime).TotalMinutes;

    // Hàm này sẽ tự động chạy khi Controller gọi ModelState.IsValid
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            // Trả về lỗi báo đỏ ngay tại ô nhập "EndTime" trên giao diện
            yield return new ValidationResult(
                "Thời gian kết thúc phải diễn ra sau thời gian bắt đầu.",
                new[] { nameof(EndTime) }
            );
        }
    }
}