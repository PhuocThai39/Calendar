using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CalendarAppointmentApp.Models;

// 1. LUẬT MỚI: Chỉ có Username là duy nhất (Không được trùng)
[Index(nameof(Username), IsUnique = true)]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên hiển thị không được để trống")]
    [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tài khoản không được để trống")]
    [StringLength(50, ErrorMessage = "Tài khoản không được vượt quá 50 ký tự")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    public string Password { get; set; } = string.Empty;

    public Calendar? Calendar { get; set; }

    public ICollection<GroupMeetingParticipant> GroupMeetingParticipants { get; set; }
        = new List<GroupMeetingParticipant>();
}