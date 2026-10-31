using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Employees;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Employees;

public class EmployeeServiceTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<ISystemCodeGenerator> _codeGeneratorMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ISecurityService> _securityServiceMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly EmployeeService _sut;

    public EmployeeServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);

        _codeGeneratorMock = new Mock<ISystemCodeGenerator>();
        _codeGeneratorMock
            .Setup(c => c.GenerateCodeAsync("Employee", It.IsAny<CancellationToken>()))
            .ReturnsAsync("NV00008");

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(u => u.UserName).Returns("admin");

        _auditServiceMock = new Mock<IAuditService>();
        _securityServiceMock = new Mock<ISecurityService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _sut = new EmployeeService(
            _context,
            _codeGeneratorMock.Object,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _securityServiceMock.Object,
            _userManagerMock.Object,
            NullLogger<EmployeeService>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithValidData_CreatesActiveEmployeeWithGeneratedCode()
    {
        // Arrange
        var dept = new Department { Code = "KHO", Name = "Kho vận" };
        var pos = new Position { Code = "NV_KHO", Name = "Nhân viên kho" };
        _context.Departments.Add(dept);
        _context.Positions.Add(pos);
        await _context.SaveChangesAsync();

        var cmd = new CreateEmployeeCommand
        {
            FullName = "Nguyễn Văn An",
            Phone = "0988111222",
            Email = "an.nguyen@tradeflow.vn",
            NationalId = "001200001111",
            DepartmentId = dept.Id,
            PositionId = pos.Id,
            Gender = "Nam",
            WorkingBranch = "Kho Lacasa",
            PayrollBranch = "Chi nhánh Hà Nội",
            DebtAndAdvance = 1500000
        };

        // Act
        var result = await _sut.CreateEmployeeAsync(cmd, "test_admin");

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("NV00008");
        result.FullName.Should().Be("Nguyễn Văn An");
        result.Status.Should().Be(EmployeeStatus.Active);
        result.DepartmentName.Should().Be("Kho vận");
        result.PositionName.Should().Be("Nhân viên kho");
        result.DebtAndAdvance.Should().Be(1500000);

        var dbEmp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        dbEmp.Should().NotBeNull();
        dbEmp!.Code.Should().Be("NV00008");
        dbEmp.Status.Should().Be(EmployeeStatus.Active);

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.EmployeeCreated,
            "test_admin",
            nameof(Employee),
            result.Code,
            It.IsAny<string>(),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEmployeesAsync_FiltersByStatus_ReturnsCorrectRecords()
    {
        // Arrange
        var emp1 = new Employee { Code = "NV00001", FullName = "Trần Anh", Status = EmployeeStatus.Active };
        var emp2 = new Employee { Code = "NV00002", FullName = "Lê Bình", Status = EmployeeStatus.Retired, TerminationReason = "Nghỉ việc" };
        _context.Employees.AddRange(emp1, emp2);
        await _context.SaveChangesAsync();

        // Act - Active
        var activeList = await _sut.GetEmployeesAsync(new EmployeeFilterDto { Status = EmployeeStatus.Active });
        // Act - Retired
        var retiredList = await _sut.GetEmployeesAsync(new EmployeeFilterDto { Status = EmployeeStatus.Retired });

        // Assert
        activeList.Should().HaveCount(1);
        activeList[0].Code.Should().Be("NV00001");

        retiredList.Should().HaveCount(1);
        retiredList[0].Code.Should().Be("NV00002");
    }

    [Fact]
    public async Task GetEmployeesAsync_SearchTerm_MatchesNameOrPhoneOrCode()
    {
        // Arrange
        var emp1 = new Employee { Code = "NV00010", FullName = "Phạm Hoàng", Phone = "0912345678", Status = EmployeeStatus.Active };
        var emp2 = new Employee { Code = "NV00020", FullName = "Đỗ Hải", Phone = "0987654321", Status = EmployeeStatus.Active };
        _context.Employees.AddRange(emp1, emp2);
        await _context.SaveChangesAsync();

        // Act
        var searchByPhone = await _sut.GetEmployeesAsync(new EmployeeFilterDto { SearchTerm = "0912345" });
        var searchByName = await _sut.GetEmployeesAsync(new EmployeeFilterDto { SearchTerm = "hải" });
        var searchByCode = await _sut.GetEmployeesAsync(new EmployeeFilterDto { SearchTerm = "NV00010" });

        // Assert
        searchByPhone.Should().HaveCount(1);
        searchByPhone[0].FullName.Should().Be("Phạm Hoàng");

        searchByName.Should().HaveCount(1);
        searchByName[0].FullName.Should().Be("Đỗ Hải");

        searchByCode.Should().HaveCount(1);
        searchByCode[0].FullName.Should().Be("Phạm Hoàng");
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ModifiesFieldsAndPreservesCode()
    {
        // Arrange
        var emp = new Employee { Code = "NV00001", FullName = "Nguyễn Văn Cũ", Phone = "0901", Status = EmployeeStatus.Active };
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        var updateCmd = new UpdateEmployeeCommand
        {
            Id = emp.Id,
            FullName = "Nguyễn Văn Mới",
            Phone = "0909999888",
            Gender = "Nam",
            Address = "Số 10 Tràng Thi, Hà Nội",
            Notes = "Cập nhật thông tin liên hệ mới"
        };

        // Act
        var updated = await _sut.UpdateEmployeeAsync(updateCmd, "admin_user");

        // Assert
        updated.FullName.Should().Be("Nguyễn Văn Mới");
        updated.Phone.Should().Be("0909999888");
        updated.Address.Should().Be("Số 10 Tràng Thi, Hà Nội");
        updated.Code.Should().Be("NV00001"); // Preserved code

        var inDb = await _context.Employees.FindAsync(emp.Id);
        inDb!.FullName.Should().Be("Nguyễn Văn Mới");

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.EmployeeUpdated,
            "admin_user",
            nameof(Employee),
            emp.Code,
            It.IsAny<string>(),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetireEmployeeAsync_ChangesStatusToRetired_AndTriggersSecurityOffboarding()
    {
        // Arrange
        var emp = new Employee
        {
            Code = "NV00003",
            FullName = "Nguyễn Thôi Việc",
            UserId = "user-acc-777",
            Status = EmployeeStatus.Active
        };
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        _securityServiceMock
            .Setup(s => s.OffboardEmployeeAsync("user-acc-777", "admin", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OffboardEmployeeResult { Succeeded = true, UserName = "hoangminh" });

        var retireCmd = new RetireEmployeeCommand
        {
            Id = emp.Id,
            TerminationDate = new DateTime(2026, 10, 1),
            TerminationReason = "Chuyển địa điểm sinh sống"
        };

        // Act
        var success = await _sut.RetireEmployeeAsync(retireCmd, "admin");

        // Assert
        success.Should().BeTrue();

        var inDb = await _context.Employees.FindAsync(emp.Id);
        inDb!.Status.Should().Be(EmployeeStatus.Retired);
        inDb.TerminationReason.Should().Be("Chuyển địa điểm sinh sống");

        // Verified Security Offboarding was triggered for the linked user
        _securityServiceMock.Verify(s => s.OffboardEmployeeAsync(
            "user-acc-777", "admin", It.Is<string>(r => r.Contains("Chuyển địa điểm sinh sống")), It.IsAny<CancellationToken>()), Times.Once);

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.EmployeeRetired,
            "admin",
            nameof(Employee),
            emp.Code,
            It.IsAny<string>(),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitEmployeeRequestAsync_CreatesPendingRequest()
    {
        // Arrange
        var reqCmd = new CreateEmployeeRequestCommand
        {
            FullName = "Vũ Đình Đề Xuất",
            Phone = "0977889900",
            Email = "dexuat@tradeflow.vn",
            WorkingBranch = "Kho Hà Đông",
            Notes = "Tuyển dụng nhân viên lái xe nâng kho"
        };

        // Act
        var result = await _sut.SubmitEmployeeRequestAsync(reqCmd, "lead_kho");

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("Vũ Đình Đề Xuất");
        result.Status.Should().Be(EmployeeRequestStatus.Pending);
        result.RequestedBy.Should().Be("lead_kho");

        var dbReq = await _context.EmployeeRequests.FirstOrDefaultAsync(r => r.Id == result.Id);
        dbReq.Should().NotBeNull();
        dbReq!.Status.Should().Be(EmployeeRequestStatus.Pending);

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.EmployeeRequestSubmitted,
            "lead_kho",
            nameof(EmployeeRequest),
            result.Id.ToString(),
            It.IsAny<string>(),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewEmployeeRequestAsync_Approve_CreatesEmployeeAndMarksRequestApproved()
    {
        // Arrange
        var request = new EmployeeRequest
        {
            FullName = "Đặng Duyệt Tuyển",
            Phone = "0933445566",
            Email = "duyet@tradeflow.vn",
            RequestedBy = "hr_officer",
            Status = EmployeeRequestStatus.Pending
        };
        _context.EmployeeRequests.Add(request);
        await _context.SaveChangesAsync();

        var reviewCmd = new ReviewEmployeeRequestCommand
        {
            RequestId = request.Id,
            Approve = true,
            ReviewNotes = "Hồ sơ đạt tiêu chuẩn, đồng ý tuyển dụng"
        };

        // Act
        var createdEmp = await _sut.ReviewEmployeeRequestAsync(reviewCmd, "director");

        // Assert
        createdEmp.Should().NotBeNull();
        createdEmp!.FullName.Should().Be("Đặng Duyệt Tuyển");
        createdEmp.Code.Should().Be("NV00008");
        createdEmp.Status.Should().Be(EmployeeStatus.Active);

        var updatedReq = await _context.EmployeeRequests.FindAsync(request.Id);
        updatedReq!.Status.Should().Be(EmployeeRequestStatus.Approved);
        updatedReq.ApprovedBy.Should().Be("director");
        updatedReq.ReviewNotes.Should().Be("Hồ sơ đạt tiêu chuẩn, đồng ý tuyển dụng");
        updatedReq.CreatedEmployeeId.Should().Be(createdEmp.Id);

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.EmployeeRequestApproved,
            "director",
            nameof(EmployeeRequest),
            request.Id.ToString(),
            It.IsAny<string>(),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewEmployeeRequestAsync_Reject_MarksRequestRejectedWithoutCreatingEmployee()
    {
        // Arrange
        var request = new EmployeeRequest
        {
            FullName = "Phan Bị Từ Chối",
            Phone = "0911223344",
            RequestedBy = "hr_officer",
            Status = EmployeeRequestStatus.Pending
        };
        _context.EmployeeRequests.Add(request);
        await _context.SaveChangesAsync();

        var initialCount = await _context.Employees.CountAsync();

        var reviewCmd = new ReviewEmployeeRequestCommand
        {
            RequestId = request.Id,
            Approve = false,
            ReviewNotes = "Hiện tại chưa có nhu cầu tuyển thêm nhân sự bộ phận này"
        };

        // Act
        var createdEmp = await _sut.ReviewEmployeeRequestAsync(reviewCmd, "director");

        // Assert
        createdEmp.Should().BeNull();

        var updatedReq = await _context.EmployeeRequests.FindAsync(request.Id);
        updatedReq!.Status.Should().Be(EmployeeRequestStatus.Rejected);
        updatedReq.ApprovedBy.Should().Be("director");
        updatedReq.ReviewNotes.Should().Be("Hiện tại chưa có nhu cầu tuyển thêm nhân sự bộ phận này");

        var currentCount = await _context.Employees.CountAsync();
        currentCount.Should().Be(initialCount); // No employee created

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.EmployeeRequestRejected,
            "director",
            nameof(EmployeeRequest),
            request.Id.ToString(),
            It.IsAny<string>(),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QuickCreateDepartmentAndPosition_SuccessfullyPersistsInDatabase()
    {
        // Act
        var dept = await _sut.CreateDepartmentAsync("MKT", "Marketing & PR", "Phòng truyền thông", "admin");
        var pos = await _sut.CreatePositionAsync("CV_MKT", "Chuyên viên PR", "Phụ trách quan hệ công chúng", "admin");

        // Assert
        dept.Should().NotBeNull();
        dept.Code.Should().Be("MKT");
        dept.Name.Should().Be("Marketing & PR");

        pos.Should().NotBeNull();
        pos.Code.Should().Be("CV_MKT");
        pos.Name.Should().Be("Chuyên viên PR");

        var inDbDept = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "MKT");
        inDbDept.Should().NotBeNull();

        var inDbPos = await _context.Positions.FirstOrDefaultAsync(p => p.Code == "CV_MKT");
        inDbPos.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateDepartmentAsync_SuccessfullyUpdatesNameAndDescription()
    {
        // Arrange
        var dept = await _sut.CreateDepartmentAsync("IT", "Phòng IT Cũ", "Mô tả cũ", "admin");

        // Act
        var updated = await _sut.UpdateDepartmentAsync(dept.Id, "IT", "Phòng Công Nghệ Thông Tin", "Phụ trách hạ tầng công nghệ", "manager");

        // Assert
        updated.Name.Should().Be("Phòng Công Nghệ Thông Tin");
        updated.Description.Should().Be("Phụ trách hạ tầng công nghệ");

        var inDb = await _context.Departments.FindAsync(dept.Id);
        inDb!.Name.Should().Be("Phòng Công Nghệ Thông Tin");
        inDb.UpdatedBy.Should().Be("manager");
    }

    [Fact]
    public async Task DeactivateAndReactivateDepartment_UpdatesIsActiveAndPreservesData()
    {
        // Arrange
        var dept = await _sut.CreateDepartmentAsync("LOGISTICS", "Hậu cần", null, "admin");

        // Act 1: Deactivate
        var deactSuccess = await _sut.DeactivateDepartmentAsync(dept.Id, "admin");
        deactSuccess.Should().BeTrue();

        var deactDept = await _context.Departments.FindAsync(dept.Id);
        deactDept!.IsActive.Should().BeFalse();

        // Active list should not include deactivated department
        var activeList = await _sut.GetDepartmentsAsync(includeInactive: false);
        activeList.Any(d => d.Id == dept.Id).Should().BeFalse();

        // Full list should include deactivated department
        var fullList = await _sut.GetDepartmentsAsync(includeInactive: true);
        var item = fullList.FirstOrDefault(d => d.Id == dept.Id);
        item.Should().NotBeNull();
        item!.IsActive.Should().BeFalse();

        // Act 2: Reactivate
        var reactSuccess = await _sut.ReactivateDepartmentAsync(dept.Id, "admin");
        reactSuccess.Should().BeTrue();

        var reactDept = await _context.Departments.FindAsync(dept.Id);
        reactDept!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDepartmentAsync_WhenNoReferences_DeletesSuccessfully()
    {
        // Arrange
        var dept = await _sut.CreateDepartmentAsync("TEMP_DEPT", "Phòng tạm", null, "admin");

        // Act
        var result = await _sut.DeleteDepartmentAsync(dept.Id, "admin");

        // Assert
        result.Should().BeTrue();
        var inDb = await _context.Departments.FindAsync(dept.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDepartmentAsync_WhenReferencedByEmployees_ThrowsInvalidOperationException()
    {
        // Arrange
        var dept = await _sut.CreateDepartmentAsync("DEPT_REF", "Phòng đang có nhân viên", null, "admin");
        var emp = new Employee
        {
            Code = "NV99991",
            FullName = "Trần Trọng Đạt",
            DepartmentId = dept.Id,
            Status = EmployeeStatus.Active
        };
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeleteDepartmentAsync(dept.Id, "admin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đang có*nhân viên*");
    }

    [Fact]
    public async Task UpdatePositionAsync_SuccessfullyUpdatesNameAndDescription()
    {
        // Arrange
        var pos = await _sut.CreatePositionAsync("TEST_POS", "Tên chức danh cũ", null, "admin");

        // Act
        var updated = await _sut.UpdatePositionAsync(pos.Id, "TEST_POS", "Tên chức danh mới", "Mô tả công việc mới", "admin");

        // Assert
        updated.Name.Should().Be("Tên chức danh mới");
        updated.Description.Should().Be("Mô tả công việc mới");

        var inDb = await _context.Positions.FindAsync(pos.Id);
        inDb!.Name.Should().Be("Tên chức danh mới");
    }

    [Fact]
    public async Task DeactivateAndReactivatePosition_UpdatesIsActiveAndPreservesData()
    {
        // Arrange
        var pos = await _sut.CreatePositionAsync("POS_DEACT", "Chức danh tạm", null, "admin");

        // Act: Deactivate
        await _sut.DeactivatePositionAsync(pos.Id, "admin");
        var deactPos = await _context.Positions.FindAsync(pos.Id);
        deactPos!.IsActive.Should().BeFalse();

        // Act: Reactivate
        await _sut.ReactivatePositionAsync(pos.Id, "admin");
        var reactPos = await _context.Positions.FindAsync(pos.Id);
        reactPos!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePositionAsync_WhenReferencedByEmployees_ThrowsInvalidOperationException()
    {
        // Arrange
        var pos = await _sut.CreatePositionAsync("POS_REF", "Chức danh có người giữ", null, "admin");
        var emp = new Employee
        {
            Code = "NV99992",
            FullName = "Lê Hoàng Nam",
            PositionId = pos.Id,
            Status = EmployeeStatus.Active
        };
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeletePositionAsync(pos.Id, "admin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Không thể xóa chức danh*tham chiếu*Ngừng sử dụng*");
    }

    [Fact]
    public async Task ReviewEmployeeRequest_WhenRequestAdditionalDocs_TransitionsToNeedAdditionalDocs()
    {
        // Arrange
        var submitCmd = new CreateEmployeeRequestCommand
        {
            FullName = "Nguyễn Văn Bổ Sung",
            Phone = "0988776655",
            Email = "bosung@tradeflow.vn"
        };
        var req = await _sut.SubmitEmployeeRequestAsync(submitCmd, "hr_user");

        // Act
        var reviewCmd = new ReviewEmployeeRequestCommand
        {
            RequestId = req.Id,
            Approve = false,
            RequestAdditionalDocs = true,
            ReviewNotes = "Thiếu bằng tốt nghiệp và CV có ảnh"
        };
        var result = await _sut.ReviewEmployeeRequestAsync(reviewCmd, "admin_user");

        // Assert
        result.Should().BeNull();
        var updated = await _context.EmployeeRequests.FindAsync(req.Id);
        updated!.Status.Should().Be(EmployeeRequestStatus.NeedAdditionalDocs);
        updated.ReviewNotes.Should().Be("Thiếu bằng tốt nghiệp và CV có ảnh");
        updated.ApprovedBy.Should().Be("admin_user");
    }

    [Fact]
    public async Task ResubmitEmployeeRequest_WhenInNeedAdditionalDocs_TransitionsBackToPending()
    {
        // Arrange
        var submitCmd = new CreateEmployeeRequestCommand
        {
            FullName = "Trần Thị Hằng",
            Phone = "0911223344"
        };
        var req = await _sut.SubmitEmployeeRequestAsync(submitCmd, "hr_user");

        await _sut.ReviewEmployeeRequestAsync(new ReviewEmployeeRequestCommand
        {
            RequestId = req.Id,
            Approve = false,
            RequestAdditionalDocs = true,
            ReviewNotes = "Cần bổ sung số CMND/CCCD"
        }, "manager");

        // Act: HR supplements info and resubmits
        var resubmitCmd = new ResubmitEmployeeRequestCommand
        {
            RequestId = req.Id,
            FullName = "Trần Thị Hằng",
            NationalId = "079198001122",
            ResubmitNotes = "Đã bổ sung CCCD chính thức"
        };
        var resubmittedDto = await _sut.ResubmitEmployeeRequestAsync(resubmitCmd, "hr_user");

        // Assert
        resubmittedDto.Status.Should().Be(EmployeeRequestStatus.Pending);
        var inDb = await _context.EmployeeRequests.FindAsync(req.Id);
        inDb!.Status.Should().Be(EmployeeRequestStatus.Pending);
        inDb.NationalId.Should().Be("079198001122");
        inDb.Notes.Should().Contain("Đã bổ sung CCCD chính thức");
    }

    [Fact]
    public async Task ReviewEmployeeRequest_WhenResubmitted_CanBeApprovedAndCreatesEmployee()
    {
        // Arrange
        var submitCmd = new CreateEmployeeRequestCommand
        {
            FullName = "Võ Minh Trí",
            Phone = "0977665544",
            Email = "tri.vo@tradeflow.vn"
        };
        var req = await _sut.SubmitEmployeeRequestAsync(submitCmd, "hr_user");

        await _sut.ReviewEmployeeRequestAsync(new ReviewEmployeeRequestCommand
        {
            RequestId = req.Id,
            Approve = false,
            RequestAdditionalDocs = true,
            ReviewNotes = "Vui lòng cập nhật email công ty"
        }, "admin");

        await _sut.ResubmitEmployeeRequestAsync(new ResubmitEmployeeRequestCommand
        {
            RequestId = req.Id,
            FullName = "Võ Minh Trí",
            Email = "tri.vominh@tradeflow.vn",
            ResubmitNotes = "Đã cập nhật email chính thức"
        }, "hr_user");

        // Act: Admin approves resubmitted request
        var approveCmd = new ReviewEmployeeRequestCommand
        {
            RequestId = req.Id,
            Approve = true,
            ReviewNotes = "Hồ sơ đạt yêu cầu"
        };
        var createdEmp = await _sut.ReviewEmployeeRequestAsync(approveCmd, "admin");

        // Assert
        createdEmp.Should().NotBeNull();
        createdEmp!.FullName.Should().Be("Võ Minh Trí");
        createdEmp.Email.Should().Be("tri.vominh@tradeflow.vn");

        var approvedReq = await _context.EmployeeRequests.FindAsync(req.Id);
        approvedReq!.Status.Should().Be(EmployeeRequestStatus.Approved);
        approvedReq.CreatedEmployeeId.Should().Be(createdEmp.Id);
    }

}
