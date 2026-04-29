using System.ComponentModel.DataAnnotations;

namespace CalendarAppointmentApp.ViewModels;

public class AddAppointmentViewModel
{
    [Required(ErrorMessage = "Tên lịch hẹn không được để trống")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa điểm không được để trống")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public bool HasReminder { get; set; }

    public DateTime? ReminderTime { get; set; }

    public string? ReminderMessage { get; set; }

    public bool ReplaceOldAppointment { get; set; }

    public bool JoinGroupMeeting { get; set; }

    public int? ConflictAppointmentId { get; set; }

    public int? MatchingGroupMeetingId { get; set; }
}