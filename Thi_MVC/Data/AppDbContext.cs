using CalendarAppointmentApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarAppointmentApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Calendar> Calendars => Set<Calendar>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<GroupMeeting> GroupMeetings => Set<GroupMeeting>();
    public DbSet<GroupMeetingParticipant> GroupMeetingParticipants => Set<GroupMeetingParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // 2. Cấu hình các mối quan hệ (Relationships) & Cascade Delete
        modelBuilder.Entity<User>()
            .HasOne(u => u.Calendar)
            .WithOne(c => c.User)
            .HasForeignKey<Calendar>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa User tự động xóa Calendar

        modelBuilder.Entity<Calendar>()
            .HasMany(c => c.Appointments)
            .WithOne(a => a.Calendar)
            .HasForeignKey(a => a.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasMany(a => a.Reminders)
            .WithOne(r => r.Appointment)
            .HasForeignKey(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Khóa chính kép cho bảng trung gian GroupMeetingParticipant
        modelBuilder.Entity<GroupMeetingParticipant>()
            .HasKey(x => new { x.UserId, x.GroupMeetingId });

        modelBuilder.Entity<GroupMeetingParticipant>()
            .HasOne(x => x.User)
            .WithMany(u => u.GroupMeetingParticipants)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMeetingParticipant>()
            .HasOne(x => x.GroupMeeting)
            .WithMany(g => g.Participants)
            .HasForeignKey(x => x.GroupMeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Dữ liệu mẫu khởi tạo (Seed Data)
        modelBuilder.Entity<User>().HasData(
        new User { Id = 1, Name = "Nguyên Khang", Username = "khang", Password = "123" },
        new User { Id = 2, Name = "Xuân Mạnh", Username = "manh", Password = "123" },
        new User { Id = 3, Name = "Quốc Trung", Username = "trung", Password = "123" }
    );

        modelBuilder.Entity<Calendar>().HasData(
            new Calendar { Id = 1, UserId = 1 },
            new Calendar { Id = 2, UserId = 2 },
            new Calendar { Id = 3, UserId = 3 }
        );

        modelBuilder.Entity<GroupMeeting>().HasData(new GroupMeeting
        {
            Id = 1,
            Name = "Sprint Planning",
            StartTime = new DateTime(2026, 5, 1, 9, 0, 0),
            EndTime = new DateTime(2026, 5, 1, 10, 0, 0)
        });
    }
}