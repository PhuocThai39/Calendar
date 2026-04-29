using System.ComponentModel.DataAnnotations;

namespace CalendarAppointmentApp.Models;

public class User
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public Calendar? Calendar { get; set; }

    public ICollection<GroupMeetingParticipant> GroupMeetingParticipants { get; set; }
        = new List<GroupMeetingParticipant>();
}