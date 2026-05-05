using CalendarAppointmentApp.Data;
using CalendarAppointmentApp.Models;
using CalendarAppointmentApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CalendarAppointmentApp.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly AppDbContext _context;

    // 1. ĐÃ ĐIỀU CHỈNH: Sửa lại hàm lấy ID để không bị lỗi khi người dùng chưa đăng nhập
    private int? CurrentUserId
    {
        get
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : null;
        }
    }

    public AppointmentsController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var today = DateTime.Today;
        int selectedMonth = month ?? today.Month;
        int selectedYear = year ?? today.Year;

        var firstDayOfMonth = new DateTime(selectedYear, selectedMonth, 1);
        int diff = (int)firstDayOfMonth.DayOfWeek;
        var calendarStart = firstDayOfMonth.AddDays(-diff);
        var calendarEnd = calendarStart.AddDays(35);

        // ĐÃ SỬA: Tạo một list chung để hứng cả 2 loại lịch
        var allAppointments = new List<AppointmentListViewModel>();

        if (CurrentUserId.HasValue)
        {
            // 1. GOM LỊCH CÁ NHÂN VÀO RỔ
            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.UserId == CurrentUserId.Value);
            if (calendar != null)
            {
                var personalAppointments = await _context.Appointments
                    .Where(a => a.CalendarId == calendar.Id &&
                                a.StartTime.Date >= calendarStart.Date &&
                                a.StartTime.Date < calendarEnd.Date)
                    .ToListAsync();

                allAppointments.AddRange(personalAppointments.Select(a => new AppointmentListViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsGroupMeeting = false
                }));
            }

            // 2. GOM LỊCH HỌP NHÓM VÀO RỔ
            var groupMeetings = await _context.GroupMeetingParticipants
                .Include(p => p.GroupMeeting)
                .Where(p => p.UserId == CurrentUserId.Value &&
                            p.GroupMeeting.StartTime.Date >= calendarStart.Date &&
                            p.GroupMeeting.StartTime.Date < calendarEnd.Date)
                .Select(p => p.GroupMeeting)
                .ToListAsync();

            allAppointments.AddRange(groupMeetings.Select(g => new AppointmentListViewModel
            {
                Id = g.Id,
                Name = g.Name,
                StartTime = g.StartTime,
                EndTime = g.EndTime,
                IsGroupMeeting = true
            }));
        }

        var days = new List<CalendarDayViewModel>();
        for (int i = 0; i < 35; i++)
        {
            var date = calendarStart.AddDays(i);
            days.Add(new CalendarDayViewModel
            {
                Date = date,
                IsCurrentMonth = date.Month == selectedMonth,
                // ĐÃ SỬA: Lọc từ rổ chung ra từng ngày và sắp xếp theo giờ
                Appointments = allAppointments
                    .Where(a => a.StartTime.Date == date.Date)
                    .OrderBy(a => a.StartTime)
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

    [HttpGet]
    public IActionResult Create(DateTime? date)
    {
        // Lấy giờ hiện tại
        var now = DateTime.Now;

        // LÀM SẠCH: Tạo ra một DateTime mới, giữ nguyên Năm, Tháng, Ngày, Giờ, Phút nhưng ép phần Giây về số 0
        var cleanNow = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        // Dùng giờ đã làm sạch để cộng thêm 1 tiếng làm mặc định
        var start = date?.Date.AddHours(8) ?? cleanNow.AddHours(1);

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
        // 1. Kiểm tra tính hợp lệ cơ bản
        if (vm.StartTime < DateTime.Now) ModelState.AddModelError("StartTime", "Thời gian bắt đầu không được nằm trong quá khứ.");
        if (vm.EndTime <= vm.StartTime) ModelState.AddModelError("EndTime", "Thời gian kết thúc phải diễn ra sau thời gian bắt đầu.");
        if (vm.HasReminder && vm.ReminderMinutesBefore == null) ModelState.AddModelError("ReminderMinutesBefore", "Vui lòng chọn khoảng thời gian nhắc trước.");
        if (!ModelState.IsValid) return View(vm);

        var calendar = await _context.Calendars.Include(c => c.Appointments).FirstOrDefaultAsync(c => c.UserId == CurrentUserId.Value);
        if (calendar == null)
        {
            calendar = new Calendar { UserId = CurrentUserId.Value };
            _context.Calendars.Add(calendar);
            await _context.SaveChangesAsync();
        }

        // ĐÃ THÊM MỚI: 2. Kiểm tra xem User có đang vướng lịch HỌP NHÓM nào khác không (Cá nhân vs Nhóm / Nhóm vs Nhóm)
        var busyGroup = await _context.GroupMeetingParticipants
            .Include(p => p.GroupMeeting)
            .FirstOrDefaultAsync(p => p.UserId == CurrentUserId.Value &&
                                      p.GroupMeeting.StartTime < vm.EndTime &&
                                      vm.StartTime < p.GroupMeeting.EndTime);

        if (busyGroup != null)
        {
            // Trừ khi họ đang bấm Join vào CHÍNH cái nhóm đó 
            if (!(vm.JoinGroupMeeting == true && vm.MatchingGroupMeetingId == busyGroup.GroupMeetingId))
            {
                // ĐÃ SỬA: Hiển thị đầy đủ Ngày/Tháng/Năm để user biết có phải lịch qua ngày không
                ModelState.AddModelError(string.Empty, $"Trùng lịch! Bạn đang có lịch họp nhóm '{busyGroup.GroupMeeting.Name}' từ {busyGroup.GroupMeeting.StartTime:dd/MM/yyyy HH:mm} đến {busyGroup.GroupMeeting.EndTime:dd/MM/yyyy HH:mm}. Bạn không thể ghi đè lịch nhóm, vui lòng đổi thời gian khác.");
                return View(vm);
            }
        }

        // 3. Kiểm tra Trùng lịch CÁ NHÂN (Cá nhân vs Cá nhân / Nhóm vs Cá nhân)
        var conflictAppointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.CalendarId == calendar.Id && a.StartTime < vm.EndTime && vm.StartTime < a.EndTime);

        if (conflictAppointment != null && !vm.ReplaceOldAppointment)
        {
            ModelState.Clear();
            vm.ConflictAppointmentId = conflictAppointment.Id;
            TempData["Warning"] = $"Bạn đã có lịch hẹn cá nhân '{conflictAppointment.Name}' trong khoảng thời gian này. Hãy xác nhận ghi đè hoặc đổi thời gian khác.";
            return View(vm);
        }

        // 4. GỢI Ý JOIN NHÓM (Dựa theo Địa điểm và Thời gian)
        var suggestGroup = await _context.GroupMeetings
            .FirstOrDefaultAsync(g =>
                g.Location.ToLower() == vm.Location.ToLower() &&
                g.StartTime < vm.EndTime && vm.StartTime < g.EndTime);

        if (suggestGroup != null && !vm.JoinGroupMeeting.HasValue)
        {
            // Tránh gợi ý nếu User đã ở trong nhóm đó rồi
            var alreadyIn = await _context.GroupMeetingParticipants.AnyAsync(p => p.UserId == CurrentUserId.Value && p.GroupMeetingId == suggestGroup.Id);
            if (!alreadyIn)
            {
                ModelState.Clear();
                vm.MatchingGroupMeetingId = suggestGroup.Id;
                vm.ConflictGroupMeetingName = suggestGroup.Name;
                return View(vm);
            }
        }

        // --- NẾU VƯỢT QUA HẾT CÁC BƯỚC TRÊN, TIẾN HÀNH LƯU VÀO DATABASE ---

        // Xóa lịch cá nhân cũ nếu có check Ghi đè
        if (conflictAppointment != null && vm.ReplaceOldAppointment)
        {
            _context.Appointments.Remove(conflictAppointment);
            await _context.SaveChangesAsync();
        }

        // 5. Xử lý Join
        if (vm.MatchingGroupMeetingId.HasValue && vm.JoinGroupMeeting == true)
        {
            bool alreadyJoined = await _context.GroupMeetingParticipants.AnyAsync(x => x.UserId == CurrentUserId.Value && x.GroupMeetingId == vm.MatchingGroupMeetingId.Value);
            if (!alreadyJoined)
            {
                _context.GroupMeetingParticipants.Add(new GroupMeetingParticipant { UserId = CurrentUserId.Value, GroupMeetingId = vm.MatchingGroupMeetingId.Value });
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Bạn đã tham gia cuộc họp nhóm thành công.";
            return RedirectToAction(nameof(Index));
        }

        // 6. Xử lý tạo mới
        if (vm.IsStartNewGroupMeeting)
        {
            var newGroup = new GroupMeeting { Name = vm.Name, Location = vm.Location, StartTime = vm.StartTime, EndTime = vm.EndTime };
            _context.GroupMeetings.Add(newGroup);
            await _context.SaveChangesAsync();

            _context.GroupMeetingParticipants.Add(new GroupMeetingParticipant { UserId = CurrentUserId.Value, GroupMeetingId = newGroup.Id });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bắt đầu cuộc họp nhóm mới thành công.";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            var appointment = new Appointment { Name = vm.Name, Location = vm.Location, StartTime = vm.StartTime, EndTime = vm.EndTime, CalendarId = calendar.Id };
            if (vm.HasReminder)
            {
                appointment.Reminders.Add(new Reminder { MinutesBefore = vm.ReminderMinutesBefore!.Value, Message = string.IsNullOrWhiteSpace(vm.ReminderMessage) ? $"Nhắc lịch hẹn: {vm.Name}" : vm.ReminderMessage });
            }
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm lịch hẹn cá nhân thành công.";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Details(int id, bool isGroup = false)
    {
        AppointmentListViewModel? vm = null;

        if (!isGroup)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Reminders)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment != null)
            {
                vm = new AppointmentListViewModel
                {
                    Id = appointment.Id,
                    Name = appointment.Name,
                    Location = appointment.Location,
                    StartTime = appointment.StartTime,
                    EndTime = appointment.EndTime,
                    IsGroupMeeting = false,
                    ReminderMessages = appointment.Reminders.Select(r => $"Nhắc trước {r.MinutesBefore} phút: {r.Message}").ToList()
                };
            }
        }
        else
        {
            var groupMeeting = await _context.GroupMeetings.FirstOrDefaultAsync(g => g.Id == id);

            if (groupMeeting != null)
            {
                // ĐÃ THÊM: Kéo danh sách thành viên dựa vào bảng trung gian
                var participants = await _context.GroupMeetingParticipants
                    .Include(p => p.User)
                    .Where(p => p.GroupMeetingId == id)
                    .Select(p => new ParticipantInfo
                    {
                        Name = p.User.Name,
                        Username = p.User.Username
                    })
                    .ToListAsync();

                vm = new AppointmentListViewModel
                {
                    Id = groupMeeting.Id,
                    Name = groupMeeting.Name,
                    Location = groupMeeting.Location,
                    StartTime = groupMeeting.StartTime,
                    EndTime = groupMeeting.EndTime,
                    IsGroupMeeting = true,
                    ReminderMessages = new List<string>(),
                    Participants = participants // Gán danh sách vào ViewModel
                };
            }
        }

        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var viewModels = new List<AppointmentListViewModel>();

        // 1. LẤY LỊCH CÁ NHÂN
        var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.UserId == CurrentUserId);
        if (calendar != null)
        {
            var personalAppointments = await _context.Appointments
                .Where(a => a.CalendarId == calendar.Id)
                .ToListAsync();

            viewModels.AddRange(personalAppointments.Select(a => new AppointmentListViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Location = a.Location,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                IsGroupMeeting = false
            }));
        }

        // 2. LẤY LỊCH HỌP NHÓM MÀ USER NÀY THAM GIA
        if (CurrentUserId.HasValue)
        {
            var groupMeetings = await _context.GroupMeetingParticipants
                .Include(p => p.GroupMeeting)
                .Where(p => p.UserId == CurrentUserId.Value)
                .Select(p => p.GroupMeeting)
                .ToListAsync();

            viewModels.AddRange(groupMeetings.Select(g => new AppointmentListViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Location = g.Location,
                StartTime = g.StartTime,
                EndTime = g.EndTime,
                IsGroupMeeting = true
            }));
        }

        // 3. TRỘN CHUNG VÀ SẮP XẾP TỪ GẦN NHẤT TỚI XA NHẤT
        var sortedList = viewModels.OrderBy(x => x.StartTime).ToList();

        return View(sortedList);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool isGroup = false) // ĐÃ THÊM: Nhận cờ isGroup
    {
        if (!isGroup)
        {
            // === XÓA LỊCH CÁ NHÂN ===
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa lịch cá nhân thành công.";
            }
        }
        else
        {
            // === XÓA CUỘC HỌP NHÓM ===
            var groupMeeting = await _context.GroupMeetings.FindAsync(id);
            if (groupMeeting != null)
            {
                _context.GroupMeetings.Remove(groupMeeting);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy cuộc họp nhóm thành công.";
            }
        }

        // Sau khi xóa, quay trở lại trang Danh sách
        return RedirectToAction(nameof(List));
    }

    // 1. GET: Hiển thị Form chỉnh sửa (Phân luồng Cá nhân / Nhóm)
    [HttpGet]
    public async Task<IActionResult> Edit(int id, bool isGroup = false)
    {
        var vm = new AddAppointmentViewModel { IsGroupEdit = isGroup };

        if (!isGroup)
        {
            // Lấy Lịch cá nhân
            var appointment = await _context.Appointments.Include(a => a.Reminders).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();

            var reminder = appointment.Reminders.FirstOrDefault();
            vm.Name = appointment.Name; vm.Location = appointment.Location;
            vm.StartTime = appointment.StartTime; vm.EndTime = appointment.EndTime;
            vm.HasReminder = reminder != null;
            vm.ReminderMinutesBefore = reminder?.MinutesBefore;
            vm.ReminderMessage = reminder?.Message;
        }
        else
        {
            // Lấy Lịch Nhóm
            var groupMeeting = await _context.GroupMeetings.FirstOrDefaultAsync(g => g.Id == id);
            if (groupMeeting == null) return NotFound();

            vm.Name = groupMeeting.Name; vm.Location = groupMeeting.Location;
            vm.StartTime = groupMeeting.StartTime; vm.EndTime = groupMeeting.EndTime;
            // Lịch nhóm hiện tại chưa có Reminder nên để trống
        }

        return View(vm);
    }

    // 2. POST: Xử lý lưu dữ liệu (Lưu vào đúng bảng)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AddAppointmentViewModel vm)
    {
        if (vm.StartTime < DateTime.Now) ModelState.AddModelError("StartTime", "Thời gian bắt đầu không được trong quá khứ.");
        if (vm.EndTime <= vm.StartTime) ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu.");
        if (!ModelState.IsValid) return View(vm);

        if (!vm.IsGroupEdit)
        {
            // === CẬP NHẬT LỊCH CÁ NHÂN ===
            var appointment = await _context.Appointments.Include(a => a.Reminders).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();

            // K.Tra xem có lấn vào lịch HỌP NHÓM nào không
            var busyGroup = await _context.GroupMeetingParticipants
                .Include(p => p.GroupMeeting)
                .FirstOrDefaultAsync(p => p.UserId == CurrentUserId.Value &&
                                          p.GroupMeeting.StartTime < vm.EndTime &&
                                          vm.StartTime < p.GroupMeeting.EndTime);
            if (busyGroup != null)
            {
                // ĐÃ SỬA: Thêm thời gian cụ thể
                ModelState.AddModelError(string.Empty, $"Trùng lịch! Bạn đang có lịch họp nhóm '{busyGroup.GroupMeeting.Name}' (từ {busyGroup.GroupMeeting.StartTime:dd/MM/yyyy HH:mm} đến {busyGroup.GroupMeeting.EndTime:dd/MM/yyyy HH:mm}). Không thể dời lịch cá nhân vào đây.");
                return View(vm);
            }

            // K.Tra trùng lịch CÁ NHÂN khác
            var conflict = await _context.Appointments.FirstOrDefaultAsync(a => a.Id != id && a.CalendarId == appointment.CalendarId && a.StartTime < vm.EndTime && vm.StartTime < a.EndTime);
            if (conflict != null && !vm.ReplaceOldAppointment)
            {
                vm.ConflictAppointmentId = conflict.Id;
                TempData["Warning"] = $"Trùng lịch với '{conflict.Name}'. Bạn có muốn ghi đè không?";
                return View(vm);
            }

            // ĐÃ SỬA: Thực thi xóa lịch cũ nếu User đồng ý Ghi đè
            if (conflict != null && vm.ReplaceOldAppointment)
            {
                _context.Appointments.Remove(conflict);
            }

            appointment.Name = vm.Name; appointment.Location = vm.Location;
            appointment.StartTime = vm.StartTime; appointment.EndTime = vm.EndTime;

            _context.Reminders.RemoveRange(appointment.Reminders);
            if (vm.HasReminder)
            {
                appointment.Reminders.Add(new Reminder { MinutesBefore = vm.ReminderMinutesBefore!.Value, Message = vm.ReminderMessage ?? $"Nhắc: {vm.Name}" });
            }
        }
        else
        {
            // === CẬP NHẬT LỊCH NHÓM ===
            var groupMeeting = await _context.GroupMeetings.FirstOrDefaultAsync(g => g.Id == id);
            if (groupMeeting == null) return NotFound();

            // K.Tra xem có lấn vào lịch HỌP NHÓM KHÁC không
            var busyGroup = await _context.GroupMeetingParticipants
                .Include(p => p.GroupMeeting)
                .FirstOrDefaultAsync(p => p.UserId == CurrentUserId.Value &&
                                          p.GroupMeetingId != id && // Bỏ qua chính nhóm đang sửa
                                          p.GroupMeeting.StartTime < vm.EndTime &&
                                          vm.StartTime < p.GroupMeeting.EndTime);
            if (busyGroup != null)
            {
                // ĐÃ SỬA: Thêm thời gian cụ thể
                ModelState.AddModelError(string.Empty, $"Trùng lịch! Bạn đang tham gia một nhóm khác ('{busyGroup.GroupMeeting.Name}' từ {busyGroup.GroupMeeting.StartTime:dd/MM/yyyy HH:mm} đến {busyGroup.GroupMeeting.EndTime:dd/MM/yyyy HH:mm}). Không thể dời lịch.");
                return View(vm);
            }

            groupMeeting.Name = vm.Name; groupMeeting.Location = vm.Location;
            groupMeeting.StartTime = vm.StartTime; groupMeeting.EndTime = vm.EndTime;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật lịch hẹn thành công!";

        return RedirectToAction(nameof(Details), new { id = id, isGroup = vm.IsGroupEdit });
    }
}