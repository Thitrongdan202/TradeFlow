using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TradeFlow.Domain.Entities.Employees;
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

        _sut = new NotificationService(new TestDbContextFactory(dbOptions), NullLogger<NotificationService>.Instance);
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

    [Fact]
    public async Task GetNotificationsAsync_WhenPendingEmployeeRequest_SurfacesApprovalNotification()
    {
        // Arrange
        var empReq = new EmployeeRequest
        {
            FullName = "Nguyễn Văn Đề Xuất",
            Code = "NV-2026-0099",
            Status = EmployeeRequestStatus.Pending,
            RequestedBy = "truong_phong_hr",
            CreatedAt = DateTime.UtcNow
        };
        _context.EmployeeRequests.Add(empReq);
        await _context.SaveChangesAsync();

        var adminIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Name, "administrator"),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "TestAuth");
        var adminUser = new ClaimsPrincipal(adminIdentity);

        // Act
        var notifications = await _sut.GetNotificationsAsync(adminUser);

        // Assert
        notifications.Should().Contain(n => n.IsApprovalRequest && n.RequestType == "EmployeeRequest");
        var approvalNotif = notifications.First(n => n.RequestType == "EmployeeRequest");
        approvalNotif.RequestCode.Should().Be("NV-2026-0099");
        approvalNotif.Requester.Should().Be("truong_phong_hr");
        approvalNotif.CanApprove.Should().BeTrue();
        approvalNotif.IsSelfRequest.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveNotificationAsync_WhenSelfApproval_RejectsWithSelfApprovalMessage()
    {
        // Arrange: Requester is "user_applicant" who also tries to approve their own request
        var empReq = new EmployeeRequest
        {
            FullName = "Tự ứng cử",
            Code = "NV-SELF-01",
            Status = EmployeeRequestStatus.Pending,
            RequestedBy = "user_applicant",
            CreatedAt = DateTime.UtcNow
        };
        _context.EmployeeRequests.Add(empReq);
        await _context.SaveChangesAsync();

        var selfIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-self-1"),
            new Claim(ClaimTypes.Name, "user_applicant"),
            new Claim(ClaimTypes.Role, "Administrator") // Even if admin, self-approval is blocked
        }, "TestAuth");
        var selfUser = new ClaimsPrincipal(selfIdentity);

        // Act & Assert
        var act = () => _sut.ApproveNotificationAsync($"appr_emp_{empReq.Id}", selfUser);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tự phê duyệt*");

        // Verify request is still pending
        var inDb = await _context.EmployeeRequests.FindAsync(empReq.Id);
        inDb!.Status.Should().Be(EmployeeRequestStatus.Pending);
    }

    [Fact]
    public async Task ApproveNotificationAsync_WhenAuthorizedAdmin_ApprovesSuccessfully()
    {
        // Arrange
        var empReq = new EmployeeRequest
        {
            FullName = "Nguyễn Nhân Viên",
            Code = "NV-2026-0088",
            Status = EmployeeRequestStatus.Pending,
            RequestedBy = "staff_requester",
            CreatedAt = DateTime.UtcNow
        };
        _context.EmployeeRequests.Add(empReq);
        await _context.SaveChangesAsync();

        var adminIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Name, "admin_boss"),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "TestAuth");
        var adminUser = new ClaimsPrincipal(adminIdentity);

        // Act
        var result = await _sut.ApproveNotificationAsync($"appr_emp_{empReq.Id}", adminUser);

        // Assert
        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var inDb = await _context.EmployeeRequests.FindAsync(empReq.Id);
        inDb!.Status.Should().Be(EmployeeRequestStatus.Approved);
        inDb.ApprovedBy.Should().Be("admin_boss");
    }

    [Fact]
    public async Task RejectNotificationAsync_WhenAuthorizedAdmin_RejectsSuccessfully()
    {
        // Arrange
        var empReq = new EmployeeRequest
        {
            FullName = "Nhân Viên Không Đạt",
            Code = "NV-2026-0077",
            Status = EmployeeRequestStatus.Pending,
            RequestedBy = "staff_requester",
            CreatedAt = DateTime.UtcNow
        };
        _context.EmployeeRequests.Add(empReq);
        await _context.SaveChangesAsync();

        var adminIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Name, "admin_boss"),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "TestAuth");
        var adminUser = new ClaimsPrincipal(adminIdentity);

        // Act
        var result = await _sut.RejectNotificationAsync($"appr_emp_{empReq.Id}", "Thông tin chưa đầy đủ", adminUser);

        // Assert
        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var inDb = await _context.EmployeeRequests.FindAsync(empReq.Id);
        inDb!.Status.Should().Be(EmployeeRequestStatus.Rejected);
        inDb.ReviewNotes.Should().Be("Thông tin chưa đầy đủ");
    }

    private class TestDbContextFactory : IDbContextFactory<TradeFlowDbContext>
    {
        private readonly DbContextOptions<TradeFlowDbContext> _options;
        public TestDbContextFactory(DbContextOptions<TradeFlowDbContext> options) => _options = options;
        public TradeFlowDbContext CreateDbContext() => new TradeFlowDbContext(_options);
        public Task<TradeFlowDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) 
            => Task.FromResult(new TradeFlowDbContext(_options));
    }

    [Fact]
    public async Task GetNotificationsAsync_WithDbContextFactory_SupportsConcurrentExecution()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var factory = new TestDbContextFactory(options);
        var sut = new NotificationService(factory, NullLogger<NotificationService>.Instance);

        // Seed data using an isolated context
        await using (var seedCtx = factory.CreateDbContext())
        {
            seedCtx.EmployeeRequests.Add(new EmployeeRequest
            {
                FullName = "Concurrent Tester",
                Code = "NV-CONC-01",
                Status = EmployeeRequestStatus.Pending,
                RequestedBy = "requester",
                CreatedAt = DateTime.UtcNow
            });
            await seedCtx.SaveChangesAsync();
        }

        // Act - Simulate simultaneous background notification menu polling and multiple page loads
        var tasks = Enumerable.Range(0, 10).Select(_ => sut.GetNotificationsAsync());
        var results = await Task.WhenAll(tasks);

        // Assert - All 10 concurrent executions succeed with zero ConcurrencyDetector exceptions
        results.Should().HaveCount(10);
        foreach (var list in results)
        {
            list.Should().ContainSingle(x => x.RequestCode == "NV-CONC-01");
        }
    }
}
