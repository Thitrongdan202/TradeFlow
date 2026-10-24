using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Entities.Contracts;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Contracts;

public class EmploymentContractTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly Mock<ILogger<EmploymentContractService>> _mockLogger;
    private readonly EmploymentContractService _sut;
    private readonly Employee _testEmployee;

    public EmploymentContractTests()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new TradeFlowDbContext(options);
        _mockAudit = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<EmploymentContractService>>();

        _sut = new EmploymentContractService(_context, _mockAudit.Object, _mockLogger.Object);

        // Seed Party A settings
        _context.ContractPartyASettings.Add(new ContractPartyASettings
        {
            CompanyName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
            TaxCode = "0316899999",
            RegisteredAddress = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Cần Thơ",
            RepresentativeName = "Nguyễn Văn An",
            RepresentativePosition = "Giám đốc",
            Phone = "0909 123 456"
        });

        // Seed Category
        _context.DocumentCategories.Add(new DocumentCategory
        {
            Code = "HD_LAO_DONG",
            Name = "Hợp đồng lao động",
            Group = DocumentTypeCategory.Internal,
            IsActive = true
        });

        // Seed test employee
        _testEmployee = new Employee
        {
            Code = "NV00001",
            FullName = "Nguyễn Văn Tuấn",
            DateOfBirth = new DateTime(1992, 5, 15),
            Gender = "Nam",
            NationalId = "079092001234",
            Address = "Số 10 Lý Tự Trọng, Cần Thơ",
            Phone = "0912 345 678",
            Email = "tuan.nv@lacasavietnam.vn",
            BankInformation = "123456789 - Vietcombank CN Cần Thơ",
            Status = EmployeeStatus.Active
        };
        _context.Employees.Add(_testEmployee);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateContract_IndefiniteTerm_SuccessAndSnapshotsData()
    {
        // Arrange
        var dto = new CreateEmploymentContractDto
        {
            EmployeeId = _testEmployee.Id,
            ContractType = EmploymentContractType.IndefiniteTerm,
            ContractDate = DateTime.UtcNow,
            ContractPlace = "Cần Thơ",
            StartDate = DateTime.UtcNow.Date,
            JobTitle = "Kỹ sư giám sát",
            DepartmentName = "Khối Kỹ thuật",
            BasicSalary = 20_000_000m,
            SalaryType = "Gross",
            WorkingHoursPerWeek = 40,
            HasNonDisclosureAgreement = true,
            NdaClauseDetails = "Cam kết bảo mật kỹ thuật và khách hàng"
        };

        // Act
        var result = await _sut.CreateContractAsync(dto, "user-1", "Admin");

        // Assert
        result.Should().NotBeNull();
        result.ContractNumber.Should().StartWith("HDLD-");
        result.Status.Should().Be(EmploymentContractStatus.Draft);
        result.ContractType.Should().Be(EmploymentContractType.IndefiniteTerm);
        result.EndDate.Should().BeNull();
        result.EmployerName.Should().Be("CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM");
        result.EmployeeFullName.Should().Be("Nguyễn Văn Tuấn");
        result.EmployeeNationalId.Should().Be("079092001234");
        result.BasicSalary.Should().Be(20_000_000m);
        result.HasNonDisclosureAgreement.Should().BeTrue();

        _mockAudit.Verify(a => a.LogAsync(
            AuditEventType.EmploymentContractCreated,
            "Admin",
            nameof(EmploymentContract),
            result.Id.ToString(),
            It.IsAny<string>(),
            null,
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task CreateContract_FixedTerm_Exceeding36Months_ThrowsException()
    {
        // Arrange: Duration 37 months exceeds Article 20 BLLĐ limit
        var startDate = DateTime.UtcNow.Date;
        var dto = new CreateEmploymentContractDto
        {
            EmployeeId = _testEmployee.Id,
            ContractType = EmploymentContractType.FixedTerm,
            StartDate = startDate,
            EndDate = startDate.AddMonths(37),
            BasicSalary = 15_000_000m
        };

        // Act & Assert
        var act = () => _sut.CreateContractAsync(dto, "user-1", "Admin");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không được vượt quá 36 tháng*");
    }

    [Fact]
    public async Task CreateContract_Probation_Exceeding180Days_ThrowsException()
    {
        // Arrange: Probation duration > 180 days exceeds Article 25 BLLĐ limit
        var startDate = DateTime.UtcNow.Date;
        var dto = new CreateEmploymentContractDto
        {
            EmployeeId = _testEmployee.Id,
            ContractType = EmploymentContractType.Probation,
            StartDate = startDate,
            EndDate = startDate.AddDays(181),
            BasicSalary = 12_000_000m
        };

        // Act & Assert
        var act = () => _sut.CreateContractAsync(dto, "user-1", "Admin");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không được vượt quá 180 ngày*");
    }

    [Fact]
    public async Task RenewContract_FixedTerm_SecondRenewal_ForcesIndefiniteTerm()
    {
        // Arrange: A contract that has already been renewed once (RenewalCount = 1)
        var parentContract = new EmploymentContract
        {
            ContractNumber = "HDLD-2025-0010",
            Title = "HỢP ĐỒNG LAO ĐỘNG",
            ContractType = EmploymentContractType.FixedTerm,
            Status = EmploymentContractStatus.Active,
            ContractDate = DateTime.UtcNow.AddYears(-1),
            ContractPlace = "Cần Thơ",
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow.Date,
            RenewalCount = 1, // Already renewed once before
            EmployerName = "LACASA",
            EmployerTaxCode = "0316899999",
            EmployerAddress = "Cần Thơ",
            EmployeeId = _testEmployee.Id,
            EmployeeCode = _testEmployee.Code,
            EmployeeFullName = _testEmployee.FullName,
            EmployeeNationalId = _testEmployee.NationalId,
            JobTitle = "Trưởng phòng Kinh doanh",
            DepartmentName = "Khối Kinh doanh",
            BasicSalary = 25_000_000m
        };
        _context.EmploymentContracts.Add(parentContract);
        await _context.SaveChangesAsync();

        var renewDto = new RenewEmploymentContractDto
        {
            ContractId = parentContract.Id,
            NewContractType = EmploymentContractType.FixedTerm, // User requested fixed-term renewal
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            BasicSalary = 28_000_000m
        };

        // Act: Under Article 20 Clause 2 BLLĐ, next renewal must convert to IndefiniteTerm
        var renewed = await _sut.RenewContractAsync(renewDto, "user-1", "HR_Manager");

        // Assert
        renewed.Should().NotBeNull();
        renewed.ContractType.Should().Be(EmploymentContractType.IndefiniteTerm, "Khoản 2 Điều 20 BLLĐ bắt buộc chuyển thành HĐLĐ không xác định thời hạn sau lần tái ký thứ 2");
        renewed.EndDate.Should().BeNull();
        renewed.RenewalCount.Should().Be(2);
        renewed.ParentContractId.Should().Be(parentContract.Id);
        renewed.BasicSalary.Should().Be(28_000_000m);
    }

    [Fact]
    public async Task GetContractById_MasksSensitiveData_WhenPermissionDenied()
    {
        // Arrange
        var contract = new EmploymentContract
        {
            ContractNumber = "HDLD-2026-9999",
            Title = "HỢP ĐỒNG LAO ĐỘNG",
            ContractType = EmploymentContractType.IndefiniteTerm,
            Status = EmploymentContractStatus.Active,
            ContractDate = DateTime.UtcNow,
            ContractPlace = "Cần Thơ",
            StartDate = DateTime.UtcNow,
            EmployerName = "LACASA",
            EmployeeId = _testEmployee.Id,
            EmployeeCode = _testEmployee.Code,
            EmployeeFullName = _testEmployee.FullName,
            EmployeeNationalId = "079092001234",
            EmployeeBankInformation = "123456789 - VCB",
            BasicSalary = 35_000_000m,
            PositionAllowance = 5_000_000m,
            LunchAllowance = 1_000_000m
        };
        _context.EmploymentContracts.Add(contract);
        await _context.SaveChangesAsync();

        // Act 1: Authorized view
        var authorizedView = await _sut.GetContractByIdAsync(contract.Id, canViewSensitiveData: true);

        // Act 2: Unauthorized view
        var maskedView = await _sut.GetContractByIdAsync(contract.Id, canViewSensitiveData: false);

        // Assert 1: Authorized
        authorizedView.Should().NotBeNull();
        authorizedView!.BasicSalary.Should().Be(35_000_000m);
        authorizedView.PositionAllowance.Should().Be(5_000_000m);
        authorizedView.EmployeeNationalId.Should().Be("079092001234");
        authorizedView.EmployeeBankInformation.Should().Be("123456789 - VCB");
        authorizedView.CanViewSensitiveData.Should().BeTrue();

        // Assert 2: Masked
        maskedView.Should().NotBeNull();
        maskedView!.BasicSalary.Should().Be(0m);
        maskedView.PositionAllowance.Should().Be(0m);
        maskedView.LunchAllowance.Should().Be(0m);
        maskedView.EmployeeNationalId.Should().Be("********1234");
        maskedView.EmployeeBankInformation.Should().EndWith("VCB");
        maskedView.EmployeeBankInformation.Should().StartWith("***********");
        maskedView.CanViewSensitiveData.Should().BeFalse();
    }

    [Fact]
    public async Task GetContractsAsync_CalculatesExpiringSoonStatus_Dynamically()
    {
        // Arrange: Contract ending in 15 days (<= 30 days)
        var today = DateTime.UtcNow.Date;
        var contract = new EmploymentContract
        {
            ContractNumber = "HDLD-2026-0088",
            Title = "HỢP ĐỒNG LAO ĐỘNG",
            ContractType = EmploymentContractType.FixedTerm,
            Status = EmploymentContractStatus.Active, // Stored in DB as Active
            ContractDate = today.AddMonths(-11),
            ContractPlace = "Cần Thơ",
            StartDate = today.AddMonths(-11),
            EndDate = today.AddDays(15), // 15 days left
            EmployerName = "LACASA",
            EmployeeId = _testEmployee.Id,
            EmployeeCode = _testEmployee.Code,
            EmployeeFullName = _testEmployee.FullName,
            EmployeeNationalId = _testEmployee.NationalId,
            BasicSalary = 18_000_000m
        };
        _context.EmploymentContracts.Add(contract);
        await _context.SaveChangesAsync();

        // Act
        var list = await _sut.GetContractsAsync(new EmploymentContractFilterDto());

        // Assert
        var item = list.FirstOrDefault(c => c.Id == contract.Id);
        item.Should().NotBeNull();
        item!.Status.Should().Be(EmploymentContractStatus.ExpiringSoon, "Hợp đồng còn dưới 30 ngày tự động tính trạng thái Sắp hết hạn");
    }
}
