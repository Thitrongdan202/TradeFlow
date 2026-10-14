using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Settings;

public class NotificationServiceTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);

        _sut = new NotificationService(_context, NullLogger<NotificationService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetNotificationsAsync_WhenNoAuditLogs_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetNotificationsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotificationsAsync_WhenAuditLogsExist_ReturnsMappedNotifications()
    {
        // Arrange
        var backupLog = new AuditLog(
            AuditEventType.DatabaseBackupCreated,
            performedBy: "admin",
            targetEntity: "DatabaseBackup",
            targetId: "bk-1",
            details: "Sao lưu thành công",
            ipAddress: "127.0.0.1",
            userAgent: "Browser");

        var companyLog = new AuditLog(
            AuditEventType.CompanySettingsChanged,
            performedBy: "admin",
            targetEntity: "CompanySettings",
            targetId: "1",
            details: "Đã sửa tên công ty",
            ipAddress: "127.0.0.1",
            userAgent: "Browser");

        var userLockedLog = new AuditLog(
            AuditEventType.UserLocked,
            performedBy: "security_system",
            targetEntity: "User",
            targetId: "user_danger",
            details: "Khóa do nhập sai mật khẩu",
            ipAddress: "127.0.0.1",
            userAgent: "Browser");

        _context.AuditLogs.AddRange(backupLog, companyLog, userLockedLog);
        await _context.SaveChangesAsync();

        // Act
        var notifications = await _sut.GetNotificationsAsync(limit: 10);

        // Assert
        notifications.Should().HaveCount(3);
        notifications.Should().Contain(n => n.Title == "Bản sao lưu dữ liệu đã tạo");
        notifications.Should().Contain(n => n.Title == "Thông tin công ty đã cập nhật");
        notifications.Should().Contain(n => n.Title == "Cảnh báo bảo mật tài khoản" && n.Type == "warning");
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksNotificationAsReadForSpecificUser()
    {
        // Arrange
        var backupLog = new AuditLog(
            AuditEventType.DatabaseBackupCreated,
            performedBy: "admin",
            targetEntity: "DatabaseBackup",
            targetId: "bk-read-test",
            details: "Sao lưu",
            ipAddress: "127.0.0.1",
            userAgent: "Browser");

        _context.AuditLogs.Add(backupLog);
        await _context.SaveChangesAsync();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-mark-read"),
            new Claim(ClaimTypes.Name, "reader")
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var list = await _sut.GetNotificationsAsync(user);
        list.Should().HaveCount(1);
        list[0].IsRead.Should().BeFalse();

        // Act
        await _sut.MarkAsReadAsync(list[0].Id, user);
        var updatedList = await _sut.GetNotificationsAsync(user);

        // Assert
        updatedList[0].IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsAccurateCount()
    {
        // Arrange
        var log1 = new AuditLog(
            AuditEventType.DatabaseBackupCreated,
            performedBy: "admin",
            targetEntity: "DatabaseBackup",
            targetId: "bk-1",
            details: "Sao lưu",
            ipAddress: "127.0.0.1",
            userAgent: "Browser");

        var log2 = new AuditLog(
            AuditEventType.CompanySettingsChanged,
            performedBy: "admin",
            targetEntity: "CompanySettings",
            targetId: "1",
            details: "Cài đặt",
            ipAddress: "127.0.0.1",
            userAgent: "Browser");

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-count"),
            new Claim(ClaimTypes.Name, "counter")
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act & Assert Initial
        var initialUnread = await _sut.GetUnreadCountAsync(user);
        initialUnread.Should().Be(2);

        // Mark one as read
        var list = await _sut.GetNotificationsAsync(user);
        await _sut.MarkAsReadAsync(list[0].Id, user);

        var updatedUnread = await _sut.GetUnreadCountAsync(user);
        updatedUnread.Should().Be(1);
    }
}
