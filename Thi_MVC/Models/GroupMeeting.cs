using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Nhớ thêm thư viện này

namespace CalendarAppointmentApp.Models;

public class GroupMeeting : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên/chủ đề cuộc họp")]
    [StringLength(150, ErrorMessage = "Tên cuộc họp không được vượt quá 150 ký tự")]
    public string Name { get; set; } = string.Empty;

    // Bổ sung thêm địa điểm hoặc link họp (có thể cho phép null nếu chưa chốt)
    [StringLength(200)]
    public string? Location { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
    public DateTime EndTime { get; set; }

    // Danh sách người tham gia
    public ICollection<GroupMeetingParticipant> Participants { get; set; }
        = new List<GroupMeetingParticipant>();

    // Đánh dấu để EF Core bỏ qua khi tạo bảng
    [NotMapped]
    public int Duration => (int)(EndTime - StartTime).TotalMinutes;

    // Validate logic thời gian hợp lệ
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "Thời gian kết thúc cuộc họp phải diễn ra sau thời gian bắt đầu.",
                new[] { nameof(EndTime) }
            );
        }
    }
}