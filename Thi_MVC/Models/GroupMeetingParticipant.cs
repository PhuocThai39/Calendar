namespace CalendarAppointmentApp.Models;

public class GroupMeetingParticipant
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int GroupMeetingId { get; set; }
    public GroupMeeting? GroupMeeting { get; set; }
}