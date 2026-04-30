using System.ComponentModel.DataAnnotations;

namespace CalendarAppointmentApp.Models;

public class Calendar
{
    [Key]
    public int Id { get; set; }

    // Mối quan hệ 1-1 với User
    public int UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();

    // Hàm này giữ lại để dùng cho các nghiệp vụ kiểm tra in-memory (trên RAM)
    // Lưu ý: Chỉ hoạt động đúng nếu danh sách Appointments đã được Include() từ DB
    public bool CheckConflict(DateTime start, DateTime end)
    {
        return Appointments.Any(a =>
            a.StartTime < end && start < a.EndTime
        );
    }
}