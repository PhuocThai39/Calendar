using System.ComponentModel.DataAnnotations;

namespace CalendarAppointmentApp.Models;

public class GroupMeeting
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public ICollection<GroupMeetingParticipant> Participants { get; set; }
        = new List<GroupMeetingParticipant>();

    public int Duration => (int)(EndTime - StartTime).TotalMinutes;
}