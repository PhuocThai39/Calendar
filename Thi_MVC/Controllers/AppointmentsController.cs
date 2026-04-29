using CalendarAppointmentApp.Data;
using CalendarAppointmentApp.Models;
using CalendarAppointmentApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalendarAppointmentApp.Controllers;

public class AppointmentsController : Controller
{
    private readonly AppDbContext _context;

    private const int CurrentUserId = 1;

    public AppointmentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var today = DateTime.Today;

        int selectedMonth = month ?? today.Month;
        int selectedYear = year ?? today.Year;

        var firstDayOfMonth = new DateTime(selectedYear, selectedMonth, 1);

        int diff = (int)firstDayOfMonth.DayOfWeek;
        var calendarStart = firstDayOfMonth.AddDays(-diff);
        var calendarEnd = calendarStart.AddDays(42);

        var calendar = await _context.Calendars
            .FirstOrDefaultAsync(c => c.UserId == CurrentUserId);

        var appointments = new List<Appointment>();

        if (calendar != null)
        {
            appointments = await _context.Appointments
                .Where(a =>
                    a.CalendarId == calendar.Id &&
                    a.StartTime.Date >= calendarStart.Date &&
                    a.StartTime.Date < calendarEnd.Date)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }

        var days = new List<CalendarDayViewModel>();

        for (int i = 0; i < 42; i++)
        {
            var date = calendarStart.AddDays(i);

            days.Add(new CalendarDayViewModel
            {
                Date = date,
                IsCurrentMonth = date.Month == selectedMonth,
                Appointments = appointments
                    .Where(a => a.StartTime.Date == date.Date)
                    .ToList()
            });
        }

        var vm = new CalendarMonthViewModel
        {
            Month = selectedMonth,
            Year = selectedYear,
            PreviousMonth = firstDayOfMonth.AddMonths(-1),
            NextMonth = firstDayOfMonth.AddMonths(1),
            Days = days
        };

        return View(vm);
    }

    public IActionResult Create(DateTime? date)
    {
        var start = date?.Date.AddHours(8) ?? DateTime.Now.AddHours(1);

        var vm = new AddAppointmentViewModel
        {
            StartTime = start,
            EndTime = start.AddHours(1)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddAppointmentViewModel vm)
    {
        if (vm.EndTime <= vm.StartTime)
        {
            ModelState.AddModelError("", "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }

        if (vm.HasReminder && vm.ReminderTime == null)
        {
            ModelState.AddModelError("", "Vui lòng chọn thời gian nhắc nhở.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var calendar = await _context.Calendars
            .Include(c => c.Appointments)
            .FirstOrDefaultAsync(c => c.UserId == CurrentUserId);

        if (calendar == null)
        {
            calendar = new Calendar { UserId = CurrentUserId };
            _context.Calendars.Add(calendar);
            await _context.SaveChangesAsync();
        }

        var conflictAppointment = await _context.Appointments
            .FirstOrDefaultAsync(a =>
                a.CalendarId == calendar.Id &&
                a.StartTime < vm.EndTime &&
                vm.StartTime < a.EndTime);

        if (conflictAppointment != null && !vm.ReplaceOldAppointment)
        {
            vm.ConflictAppointmentId = conflictAppointment.Id;

            TempData["Warning"] =
                $"Bạn đã có lịch hẹn '{conflictAppointment.Name}' trong khoảng thời gian này. " +
                "Hãy chọn thay thế lịch cũ hoặc đổi thời gian khác.";

            return View(vm);
        }

        if (conflictAppointment != null && vm.ReplaceOldAppointment)
        {
            _context.Appointments.Remove(conflictAppointment);
            await _context.SaveChangesAsync();
        }

        var matchingGroupMeeting = await _context.GroupMeetings
    .Include(g => g.Participants)
    .FirstOrDefaultAsync(g =>
        g.Name.ToLower() == vm.Name.ToLower() &&
        g.StartTime == vm.StartTime &&
        g.EndTime == vm.EndTime);

        if (matchingGroupMeeting != null && !vm.JoinGroupMeeting)
        {
            vm.MatchingGroupMeetingId = matchingGroupMeeting.Id;
            return View(vm);
        }

        if (matchingGroupMeeting != null && vm.JoinGroupMeeting)
        {
            bool alreadyJoined = await _context.GroupMeetingParticipants.AnyAsync(x =>
                x.UserId == CurrentUserId &&
                x.GroupMeetingId == matchingGroupMeeting.Id);

            if (!alreadyJoined)
            {
                _context.GroupMeetingParticipants.Add(new GroupMeetingParticipant
                {
                    UserId = CurrentUserId,
                    GroupMeetingId = matchingGroupMeeting.Id
                });

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Bạn đã tham gia cuộc họp nhóm '{matchingGroupMeeting.Name}' thành công.";
            return RedirectToAction(nameof(Index));
        }

        var appointment = new Appointment
        {
            Name = vm.Name,
            Location = vm.Location,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            CalendarId = calendar.Id
        };

        if (vm.HasReminder)
        {
            appointment.Reminders.Add(new Reminder
            {
                Time = vm.ReminderTime!.Value,
                Message = string.IsNullOrWhiteSpace(vm.ReminderMessage)
                    ? $"Nhắc lịch hẹn: {vm.Name}"
                    : vm.ReminderMessage
            });
        }

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Thêm lịch hẹn thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
        {
            return NotFound();
        }

        return View(appointment);
    }
}