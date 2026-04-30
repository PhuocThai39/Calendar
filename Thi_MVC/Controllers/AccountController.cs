using CalendarAppointmentApp.Data;
using CalendarAppointmentApp.Models;
using CalendarAppointmentApp.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CalendarAppointmentApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // 1. ĐĂNG KÝ
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Kiểm tra username đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == vm.Username))
            {
                ModelState.AddModelError("Username", "Tài khoản này đã có người sử dụng.");
                return View(vm);
            }

            // 💡 LƯU Ý: Khi tạo User mới trong ứng dụng Lịch, thường ta sẽ cấp luôn cho họ 1 cái Lịch trống
            var user = new User
            {
                Name = vm.FullName,
                Username = vm.Username,
                Password = vm.Password,
                Calendar = new Calendar() // Tự động tạo 1 lịch mới cho user này
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                // 🎯 Lấy lỗi chi tiết từ tận cùng SQL Server đẩy lên UI
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                ModelState.AddModelError("", "Lỗi Database: " + detailedError);
                return View(vm);
            }
        }   

        // 2. ĐĂNG NHẬP
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == vm.Username && u.Password == vm.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác.");
                return View(vm);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Lưu ID
                new Claim(ClaimTypes.Name, user.Username),                // Lưu Username (Dùng cho User.Identity.Name)
    
                // ĐÃ THÊM: Lưu trường Name của bạn vào một Claim tên là "DisplayName"
                new Claim("DisplayName", user.Name)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Appointments");
        }

        // 3. ĐĂNG XUẤT
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}