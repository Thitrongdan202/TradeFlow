using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Entities.Contracts;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services.Pdf;

namespace TradeFlow.Infrastructure.Services;

public class EmploymentContractService : IEmploymentContractService
{
    private readonly TradeFlowDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<EmploymentContractService> _logger;

    public EmploymentContractService(
        TradeFlowDbContext context,
        IAuditService auditService,
        ILogger<EmploymentContractService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<EmploymentContractDto>> GetContractsAsync(
        EmploymentContractFilterDto filter,
        bool canViewSensitiveData = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmploymentContracts
            .Include(c => c.Employee)
            .AsNoTracking()
            .AsQueryable();

        // 1. Text Search
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.ContractNumber.ToLower().Contains(term) ||
                c.EmployeeFullName.ToLower().Contains(term) ||
                c.EmployeeCode.ToLower().Contains(term) ||
                c.JobTitle.ToLower().Contains(term) ||
                c.DepartmentName.ToLower().Contains(term));
        }

        // 2. Type filter
        if (filter.ContractType.HasValue)
        {
            query = query.Where(c => c.ContractType == filter.ContractType.Value);
        }

        // 3. Status filter
        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        // 4. Department filter
        if (!string.IsNullOrWhiteSpace(filter.DepartmentName))
        {
            query = query.Where(c => c.DepartmentName == filter.DepartmentName);
        }

        // 5. Date filters
        if (filter.StartDateFrom.HasValue)
        {
            query = query.Where(c => c.StartDate >= filter.StartDateFrom.Value);
        }
        if (filter.StartDateTo.HasValue)
        {
            query = query.Where(c => c.StartDate <= filter.StartDateTo.Value);
        }
        if (filter.EndDateFrom.HasValue)
        {
            query = query.Where(c => c.EndDate.HasValue && c.EndDate.Value >= filter.EndDateFrom.Value);
        }
        if (filter.EndDateTo.HasValue)
        {
            query = query.Where(c => c.EndDate.HasValue && c.EndDate.Value <= filter.EndDateTo.Value);
        }

        var entities = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var result = new List<EmploymentContractDto>();

        foreach (var entity in entities)
        {
            // Evaluate dynamic status
            var effectiveStatus = entity.Status;
            if (effectiveStatus == EmploymentContractStatus.Active && entity.EndDate.HasValue)
            {
                if (entity.EndDate.Value < now.Date)
                {
                    effectiveStatus = EmploymentContractStatus.Expired;
                }
                else if (entity.EndDate.Value <= now.Date.AddDays(30))
                {
                    effectiveStatus = EmploymentContractStatus.ExpiringSoon;
                }
            }

            var dto = MapToDto(entity, canViewSensitiveData);
            dto.Status = effectiveStatus;
            result.Add(dto);
        }

        return result;
    }

    public async Task<EmploymentContractDto?> GetContractByIdAsync(
        int id,
        bool canViewSensitiveData = true,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.EmploymentContracts
            .Include(c => c.Employee)
            .Include(c => c.ParentContract)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
            return null;

        var dto = MapToDto(entity, canViewSensitiveData);

        var now = DateTime.UtcNow;
        if (dto.Status == EmploymentContractStatus.Active && dto.EndDate.HasValue)
        {
            if (dto.EndDate.Value < now.Date)
                dto.Status = EmploymentContractStatus.Expired;
            else if (dto.EndDate.Value <= now.Date.AddDays(30))
                dto.Status = EmploymentContractStatus.ExpiringSoon;
        }

        return dto;
    }

    public async Task<EmploymentContractDto> CreateContractAsync(
        CreateEmploymentContractDto dto,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate employee
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException($"Không tìm thấy nhân viên với ID {dto.EmployeeId}");

        // 2. Position information validation
        if (string.IsNullOrWhiteSpace(dto.JobTitle))
        {
            if (dto.ContractType == EmploymentContractType.Internship || dto.ContractType == EmploymentContractType.VocationalTraining)
                throw new InvalidOperationException("Vui lòng nhập vị trí thực tập / chương trình đào tạo / nghề đào tạo.");
            if (dto.ContractType == EmploymentContractType.Probation)
                throw new InvalidOperationException("Vui lòng nhập chức danh / công việc thử việc.");
            throw new InvalidOperationException("Vui lòng nhập chức danh / vị trí công việc.");
        }

        // 3. Legal duration validation (BLLĐ 2026)
        if (dto.ContractType == EmploymentContractType.FixedTerm)
        {
            if (!dto.EndDate.HasValue)
                throw new InvalidOperationException("Hợp đồng xác định thời hạn bắt buộc phải có ngày kết thúc.");

            if (dto.EndDate.Value <= dto.StartDate)
                throw new InvalidOperationException("Ngày kết thúc phải lớn hơn ngày bắt đầu hợp đồng.");

            var months = ((dto.EndDate.Value.Year - dto.StartDate.Year) * 12) + dto.EndDate.Value.Month - dto.StartDate.Month;
            if (months > 36)
                throw new InvalidOperationException("Hợp đồng lao động xác định thời hạn không được vượt quá 36 tháng (theo Điều 20 Bộ luật Lao động).");
        }
        else if (dto.ContractType == EmploymentContractType.Probation)
        {
            if (!dto.EndDate.HasValue)
                throw new InvalidOperationException("Hợp đồng thử việc bắt buộc phải có ngày kết thúc.");

            var days = (dto.EndDate.Value - dto.StartDate).TotalDays;
            if (days > 180)
                throw new InvalidOperationException("Thời gian thử việc tối đa không được vượt quá 180 ngày (theo Điều 25 Bộ luật Lao động).");
        }
        else if (dto.ContractType == EmploymentContractType.Internship || dto.ContractType == EmploymentContractType.VocationalTraining)
        {
            if (dto.EndDate.HasValue && dto.EndDate.Value <= dto.StartDate)
                throw new InvalidOperationException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }

        // 4. Generate sequential contract number
        var prefix = dto.ContractType switch
        {
            EmploymentContractType.Probation => "HDTV",
            EmploymentContractType.Internship => "TTTT",
            EmploymentContractType.VocationalTraining => "HDDT",
            _ => "HDLD"
        };
        var year = DateTime.UtcNow.Year;
        var seqPattern = $"{prefix}-{year}-";
        var countThisYear = await _context.EmploymentContracts
            .CountAsync(c => c.ContractNumber.StartsWith(seqPattern), cancellationToken);
        var contractNumber = $"{seqPattern}{(countThisYear + 1):D4}";

        // 5. Resolve Employer Settings (Party A)
        var partyASettings = await _context.ContractPartyASettings.FirstOrDefaultAsync(cancellationToken);

        var employerName = !string.IsNullOrWhiteSpace(dto.EmployerName) ? dto.EmployerName : (partyASettings?.CompanyName ?? "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM");
        var employerTaxCode = !string.IsNullOrWhiteSpace(dto.EmployerTaxCode) ? dto.EmployerTaxCode : (partyASettings?.TaxCode ?? "0316899999");
        var employerAddress = !string.IsNullOrWhiteSpace(dto.EmployerAddress) ? dto.EmployerAddress : (partyASettings?.RegisteredAddress ?? "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ");
        var employerRepName = !string.IsNullOrWhiteSpace(dto.EmployerRepresentativeName) ? dto.EmployerRepresentativeName : (partyASettings?.RepresentativeName ?? "Trần Thị Mai Phương");
        var employerRepPosition = !string.IsNullOrWhiteSpace(dto.EmployerRepresentativePosition) ? dto.EmployerRepresentativePosition : (partyASettings?.RepresentativePosition ?? "Chủ tịch Hội đồng thành viên");
        var employerRepAuth = partyASettings?.RepresentativeAuthority ?? "Đại diện theo pháp luật";

        // Ngăn xung đột lợi ích: Người lao động không được tự ký hợp đồng với chính mình
        if (string.Equals(employee.FullName?.Trim(), employerRepName?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            employerRepName = "Trần Thị Mai Phương";
            employerRepPosition = "Chủ tịch Hội đồng thành viên";
            employerRepAuth = "Người đại diện theo pháp luật";
        }
        var employerPhone = partyASettings?.Phone ?? "0909 123 456";

        var defaultTitle = dto.ContractType switch
        {
            EmploymentContractType.Probation => "HỢP ĐỒNG THỬ VIỆC",
            EmploymentContractType.Internship => "THỎA THUẬN THỰC TẬP SINH",
            EmploymentContractType.VocationalTraining => "HỢP ĐỒNG ĐÀO TẠO NGHỀ (ĐIỀU 61 BLLĐ)",
            EmploymentContractType.IndefiniteTerm => "HỢP ĐỒNG LAO ĐỘNG (KHÔNG XÁC ĐỊNH THỜI HẠN)",
            _ => "HỢP ĐỒNG LAO ĐỘNG"
        };

        var contract = new EmploymentContract
        {
            ContractNumber = contractNumber,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? defaultTitle : dto.Title,
            ContractType = dto.ContractType,
            Status = EmploymentContractStatus.Draft,
            ContractDate = dto.ContractDate,
            ContractPlace = dto.ContractPlace,
            StartDate = dto.StartDate,
            EndDate = dto.ContractType == EmploymentContractType.IndefiniteTerm ? null : dto.EndDate,
            ProbationDurationDays = dto.ContractType == EmploymentContractType.Probation ? (dto.ProbationDurationDays ?? (int)(dto.EndDate.HasValue ? (dto.EndDate.Value - dto.StartDate).TotalDays : 60)) : null,
            ProbationSalaryPercentage = dto.ContractType == EmploymentContractType.Probation ? (dto.ProbationSalaryPercentage ?? 85m) : null,

            // Party A
            EmployerName = employerName,
            EmployerTaxCode = employerTaxCode,
            EmployerAddress = employerAddress,
            EmployerPhone = employerPhone,
            EmployerRepresentativeName = employerRepName,
            EmployerRepresentativePosition = employerRepPosition,
            EmployerRepresentativeAuthority = employerRepAuth,

            // Party B (Snapshot from Employee)
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeFullName = employee.FullName,
            EmployeeDateOfBirth = employee.DateOfBirth,
            EmployeeGender = employee.Gender,
            EmployeeNationalId = employee.NationalId,
            EmployeeAddress = employee.Address,
            EmployeePhone = employee.Phone,
            EmployeeEmail = employee.Email,
            EmployeeBankInformation = employee.BankInformation,

            // Work details
            JobTitle = dto.JobTitle,
            DepartmentName = !string.IsNullOrWhiteSpace(dto.DepartmentName) ? dto.DepartmentName : (employee.Department?.Name ?? "Khối Nghiệp vụ"),
            Workplace = dto.Workplace,
            JobDescription = dto.JobDescription,
            WorkingHoursPerWeek = dto.WorkingHoursPerWeek,
            WorkingSchedule = dto.WorkingSchedule,
            AnnualLeaveDays = dto.AnnualLeaveDays,

            // Remuneration
            BasicSalary = dto.BasicSalary,
            SalaryClauseText = dto.SalaryClauseText,
            SalaryType = dto.SalaryType,
            PaymentPeriod = dto.PaymentPeriod,
            PaymentMethod = dto.PaymentMethod,
            PositionAllowance = dto.PositionAllowance,
            LunchAllowance = dto.LunchAllowance,
            OtherAllowances = dto.OtherAllowances,
            SalaryReviewPolicy = dto.SalaryReviewPolicy,
            SocialInsuranceRate = dto.SocialInsuranceRate,
            LaborProtection = dto.LaborProtection,

            // Internship / Vocational Training
            TrainingSupervisor = dto.TrainingSupervisor,
            TrainingObjectives = dto.TrainingObjectives,
            InternshipProgramme = dto.InternshipProgramme,

            // Clauses
            HasNonDisclosureAgreement = dto.HasNonDisclosureAgreement,
            NdaClauseDetails = dto.NdaClauseDetails,
            HasTrainingReimbursement = dto.HasTrainingReimbursement,
            TrainingClauseDetails = dto.TrainingClauseDetails,
            HasSpecialBenefits = dto.HasSpecialBenefits,
            SpecialBenefitsDetails = dto.SpecialBenefitsDetails,
            CustomTerms = dto.CustomTerms,
            CustomClausesJson = (dto.Clauses != null && dto.Clauses.Any()) ? JsonSerializer.Serialize(dto.Clauses) : dto.CustomClausesJson,

            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userName
        };

        _context.EmploymentContracts.Add(contract);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractCreated,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: contract.Id.ToString(),
            details: $"Tạo hợp đồng lao động mới số {contract.ContractNumber} cho nhân viên {contract.EmployeeFullName} ({contract.EmployeeCode})",
            cancellationToken: cancellationToken);

        return MapToDto(contract, true);
    }

    public async Task<EmploymentContractDto> UpdateContractAsync(
        UpdateEmploymentContractDto dto,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);

        if (contract == null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng ID {dto.Id}");

        if (contract.Status != EmploymentContractStatus.Draft && contract.Status != EmploymentContractStatus.PendingSignature)
            throw new InvalidOperationException("Chỉ được chỉnh sửa hợp đồng ở trạng thái Bản nháp hoặc Chờ ký.");

        // Position information validation
        if (string.IsNullOrWhiteSpace(dto.JobTitle))
        {
            if (dto.ContractType == EmploymentContractType.Internship || dto.ContractType == EmploymentContractType.VocationalTraining)
                throw new InvalidOperationException("Vui lòng nhập vị trí thực tập / chương trình đào tạo / nghề đào tạo.");
            if (dto.ContractType == EmploymentContractType.Probation)
                throw new InvalidOperationException("Vui lòng nhập chức danh / công việc thử việc.");
            throw new InvalidOperationException("Vui lòng nhập chức danh / vị trí công việc.");
        }

        // Legal duration validation
        if (dto.ContractType == EmploymentContractType.FixedTerm)
        {
            if (!dto.EndDate.HasValue)
                throw new InvalidOperationException("Hợp đồng xác định thời hạn bắt buộc phải có ngày kết thúc.");

            if (dto.EndDate.Value <= dto.StartDate)
                throw new InvalidOperationException("Ngày kết thúc phải lớn hơn ngày bắt đầu hợp đồng.");

            var months = ((dto.EndDate.Value.Year - dto.StartDate.Year) * 12) + dto.EndDate.Value.Month - dto.StartDate.Month;
            if (months > 36)
                throw new InvalidOperationException("Thời hạn hợp đồng xác định thời hạn không được vượt quá 36 tháng.");
        }
        else if (dto.ContractType == EmploymentContractType.Probation)
        {
            if (!dto.EndDate.HasValue)
                throw new InvalidOperationException("Hợp đồng thử việc bắt buộc phải có ngày kết thúc.");

            var days = (dto.EndDate.Value - dto.StartDate).TotalDays;
            if (days > 180)
                throw new InvalidOperationException("Thời gian thử việc tối đa không được vượt quá 180 ngày (theo Điều 25 Bộ luật Lao động).");
        }
        else if (dto.ContractType == EmploymentContractType.Internship || dto.ContractType == EmploymentContractType.VocationalTraining)
        {
            if (dto.EndDate.HasValue && dto.EndDate.Value <= dto.StartDate)
                throw new InvalidOperationException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }

        contract.Title = dto.Title;
        if (!string.IsNullOrWhiteSpace(dto.EmployerName)) contract.EmployerName = dto.EmployerName;
        if (!string.IsNullOrWhiteSpace(dto.EmployerTaxCode)) contract.EmployerTaxCode = dto.EmployerTaxCode;
        if (!string.IsNullOrWhiteSpace(dto.EmployerAddress)) contract.EmployerAddress = dto.EmployerAddress;
        if (!string.IsNullOrWhiteSpace(dto.EmployerRepresentativeName)) contract.EmployerRepresentativeName = dto.EmployerRepresentativeName;
        if (!string.IsNullOrWhiteSpace(dto.EmployerRepresentativePosition)) contract.EmployerRepresentativePosition = dto.EmployerRepresentativePosition;

        if (dto.EmployeeId > 0 && dto.EmployeeId != contract.EmployeeId)
        {
            var newEmp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);
            if (newEmp != null)
            {
                contract.EmployeeId = newEmp.Id;
                contract.EmployeeCode = newEmp.Code;
                contract.EmployeeFullName = newEmp.FullName;
                contract.EmployeeDateOfBirth = newEmp.DateOfBirth;
                contract.EmployeeGender = newEmp.Gender;
                contract.EmployeeNationalId = newEmp.NationalId;
                contract.EmployeeAddress = newEmp.Address;
                contract.EmployeePhone = newEmp.Phone;
                contract.EmployeeEmail = newEmp.Email;
                contract.EmployeeBankInformation = newEmp.BankInformation;
            }
        }

        if (string.Equals(contract.EmployeeFullName?.Trim(), contract.EmployerRepresentativeName?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            contract.EmployerRepresentativeName = "Trần Thị Mai Phương";
            contract.EmployerRepresentativePosition = "Chủ tịch Hội đồng thành viên";
            contract.EmployerRepresentativeAuthority = "Người đại diện theo pháp luật";
        }

        contract.ContractType = dto.ContractType;
        contract.ContractDate = dto.ContractDate;
        contract.ContractPlace = dto.ContractPlace;
        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.ContractType == EmploymentContractType.IndefiniteTerm ? null : dto.EndDate;
        contract.ProbationDurationDays = dto.ProbationDurationDays;
        contract.ProbationSalaryPercentage = dto.ProbationSalaryPercentage;

        contract.JobTitle = dto.JobTitle;
        contract.DepartmentName = dto.DepartmentName;
        contract.Workplace = dto.Workplace;
        contract.JobDescription = dto.JobDescription;
        contract.WorkingHoursPerWeek = dto.WorkingHoursPerWeek;
        contract.WorkingSchedule = dto.WorkingSchedule;
        contract.AnnualLeaveDays = dto.AnnualLeaveDays;

        contract.BasicSalary = dto.BasicSalary;
        contract.SalaryClauseText = dto.SalaryClauseText;
        contract.SalaryType = dto.SalaryType;
        contract.PaymentPeriod = dto.PaymentPeriod;
        contract.PaymentMethod = dto.PaymentMethod;
        contract.PositionAllowance = dto.PositionAllowance;
        contract.LunchAllowance = dto.LunchAllowance;
        contract.OtherAllowances = dto.OtherAllowances;
        contract.SalaryReviewPolicy = dto.SalaryReviewPolicy;
        contract.SocialInsuranceRate = dto.SocialInsuranceRate;
        contract.LaborProtection = dto.LaborProtection;

        contract.TrainingSupervisor = dto.TrainingSupervisor;
        contract.TrainingObjectives = dto.TrainingObjectives;
        contract.InternshipProgramme = dto.InternshipProgramme;

        contract.HasNonDisclosureAgreement = dto.HasNonDisclosureAgreement;
        contract.NdaClauseDetails = dto.NdaClauseDetails;
        contract.HasTrainingReimbursement = dto.HasTrainingReimbursement;
        contract.TrainingClauseDetails = dto.TrainingClauseDetails;
        contract.HasSpecialBenefits = dto.HasSpecialBenefits;
        contract.SpecialBenefitsDetails = dto.SpecialBenefitsDetails;
        contract.CustomTerms = dto.CustomTerms;
        contract.CustomClausesJson = (dto.Clauses != null && dto.Clauses.Any()) ? JsonSerializer.Serialize(dto.Clauses) : dto.CustomClausesJson;
        contract.Notes = dto.Notes;

        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = userName;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractUpdated,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: contract.Id.ToString(),
            details: $"Cập nhật thông tin hợp đồng lao động số {contract.ContractNumber}",
            cancellationToken: cancellationToken);

        return MapToDto(contract, true);
    }

    public async Task<bool> FinalizeContractAsync(
        int id,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.EmploymentContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (contract == null) return false;

        if (contract.Status != EmploymentContractStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể chốt nội dung hợp đồng từ trạng thái Bản nháp.");

        // Statutory validation before finalizing
        if (contract.ContractType == EmploymentContractType.IndefiniteTerm || contract.ContractType == EmploymentContractType.FixedTerm || contract.ContractType == EmploymentContractType.Probation)
        {
            if (string.IsNullOrWhiteSpace(contract.JobTitle))
                throw new InvalidOperationException("Hợp đồng lao động yêu cầu phải có Chức danh / Vị trí công việc.");

            if ((!contract.BasicSalary.HasValue || contract.BasicSalary.Value <= 0) && string.IsNullOrWhiteSpace(contract.SalaryClauseText))
                throw new InvalidOperationException("Vui lòng hoàn thiện điều khoản tiền lương hoặc mức lương trước khi chốt hợp đồng để ký kết.");
        }
        else if (contract.ContractType == EmploymentContractType.Internship || contract.ContractType == EmploymentContractType.VocationalTraining)
        {
            if (string.IsNullOrWhiteSpace(contract.JobTitle))
                throw new InvalidOperationException("Hợp đồng/thỏa thuận đào tạo - thực tập yêu cầu vị trí hoặc chương trình đào tạo.");
        }

        contract.Status = EmploymentContractStatus.PendingSignature;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = userName;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractFinalized,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: contract.Id.ToString(),
            details: $"Chốt nội dung hợp đồng lao động số {contract.ContractNumber}, chuyển sang trạng thái Chờ ký kết",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> SignContractAsync(
        SignEmploymentContractDto dto,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.EmploymentContracts
            .FirstOrDefaultAsync(c => c.Id == dto.ContractId, cancellationToken);

        if (contract == null) return false;

        contract.SigningMethod = dto.SigningMethod;
        var now = DateTime.UtcNow;

        if (dto.SigningMethod == EmploymentSigningMethod.PaperSigned)
        {
            contract.PaperScanAttachmentPath = dto.PaperScanAttachmentPath;
            if (dto.SignerRole == "Employee")
            {
                contract.EmployeeSignedAt = now;
            }
            else if (dto.SignerRole == "Employer")
            {
                contract.EmployerSignedAt = now;
            }
            else
            {
                contract.EmployerSignedAt = now;
                contract.EmployeeSignedAt = now;
            }

            contract.Status = (contract.EmployeeSignedAt.HasValue && contract.EmployerSignedAt.HasValue)
                ? EmploymentContractStatus.Active
                : EmploymentContractStatus.PendingSignature;
        }
        else if (dto.SigningMethod == EmploymentSigningMethod.InAppDrawnSignature)
        {
            if (dto.SignerRole == "Employee")
            {
                contract.EmployeeSignatureData = dto.SignatureData;
                contract.EmployeeSignedAt = now;
            }
            else if (dto.SignerRole == "Employer")
            {
                contract.EmployerSignatureData = dto.SignatureData;
                contract.EmployerSignedAt = now;
            }
            else
            {
                contract.EmployeeSignatureData = dto.SignatureData;
                contract.EmployerSignatureData = dto.SignatureData;
                contract.EmployeeSignedAt = now;
                contract.EmployerSignedAt = now;
            }

            contract.Status = (contract.EmployeeSignedAt.HasValue && contract.EmployerSignedAt.HasValue)
                ? EmploymentContractStatus.Active
                : EmploymentContractStatus.PendingSignature;
        }
        else if (dto.SigningMethod == EmploymentSigningMethod.InternalDigitalSignature)
        {
            // Compute deterministic SHA-256 hash
            var contentToHash = $"{contract.ContractNumber}|{contract.EmployeeCode}|{contract.BasicSalary}|{contract.StartDate:O}|{contract.EmployerTaxCode}";
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(contentToHash));
            contract.DigitalSignatureHash = Convert.ToHexString(hashBytes);

            if (dto.SignerRole == "Employee")
            {
                contract.EmployeeSignatureData = "DIGITAL_SIGNATURE_SHA256";
                contract.EmployeeSignedAt = now;
            }
            else if (dto.SignerRole == "Employer")
            {
                contract.EmployerSignatureData = "DIGITAL_SIGNATURE_SHA256";
                contract.EmployerSignedAt = now;
            }
            else
            {
                contract.EmployerSignatureData = "DIGITAL_SIGNATURE_SHA256";
                contract.EmployeeSignatureData = "DIGITAL_SIGNATURE_SHA256";
                contract.EmployerSignedAt = now;
                contract.EmployeeSignedAt = now;
            }

            contract.Status = (contract.EmployeeSignedAt.HasValue && contract.EmployerSignedAt.HasValue)
                ? EmploymentContractStatus.Active
                : EmploymentContractStatus.PendingSignature;
        }
        else if (dto.SigningMethod == EmploymentSigningMethod.NationalPlatform)
        {
            contract.NationalPlatformReference = dto.NationalPlatformReference;
            if (dto.SignerRole == "Employee")
            {
                contract.EmployeeSignedAt = now;
            }
            else if (dto.SignerRole == "Employer")
            {
                contract.EmployerSignedAt = now;
            }
            else
            {
                contract.EmployerSignedAt = now;
                contract.EmployeeSignedAt = now;
            }

            contract.Status = (contract.EmployeeSignedAt.HasValue && contract.EmployerSignedAt.HasValue)
                ? EmploymentContractStatus.Active
                : EmploymentContractStatus.PendingSignature;
        }

        contract.UpdatedAt = now;
        contract.UpdatedBy = userName;

        await _context.SaveChangesAsync(cancellationToken);

        // Link with Chứng từ (DocumentAttachment)
        try
        {
            var pdfBytes = await GeneratePdfAsync(contract.Id, cancellationToken);
            var category = await _context.DocumentCategories.FirstOrDefaultAsync(c => c.Code == "HD_LAO_DONG", cancellationToken);
            
            _context.DocumentAttachments.Add(new DocumentAttachment
            {
                FileName = $"{contract.ContractNumber}.pdf",
                StoragePath = $"/documents/contracts/employment/{contract.ContractNumber}.pdf",
                FileSize = pdfBytes.Length,
                ContentType = "application/pdf",
                CategoryId = category?.Id ?? 3,
                RelatedEntityType = "EmploymentContract",
                RelatedEntityId = contract.Id,
                Description = $"Hợp đồng lao động ký kết: {contract.ContractNumber} ({contract.EmployeeFullName})",
                Status = "Active",
                CreatedAt = now,
                CreatedBy = userName
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create DocumentAttachment link for EmploymentContract {ContractNumber}", contract.ContractNumber);
        }

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractSigned,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: contract.Id.ToString(),
            details: $"Thực hiện ký hợp đồng lao động số {contract.ContractNumber} bằng phương thức {contract.SigningMethod}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<EmploymentContractDto> RenewContractAsync(
        RenewEmploymentContractDto dto,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var parent = await _context.EmploymentContracts.FirstOrDefaultAsync(c => c.Id == dto.ContractId, cancellationToken);
        if (parent == null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng gốc ID {dto.ContractId}");

        // Legal check: Article 20 Labour Code
        // If contract is fixed term and already renewed once, must convert to indefinite term
        var newType = dto.NewContractType;
        if (parent.ContractType == EmploymentContractType.FixedTerm && parent.RenewalCount >= 1 && newType == EmploymentContractType.FixedTerm)
        {
            _logger.LogInformation("Theo Khoản 2 Điều 20 BLLĐ, HĐLĐ xác định thời hạn đã tái ký 1 lần, lần tiếp theo phải chuyển thành HĐLĐ không xác định thời hạn.");
            newType = EmploymentContractType.IndefiniteTerm;
        }

        var year = DateTime.UtcNow.Year;
        var seqPattern = $"HDLD-{year}-";
        var countThisYear = await _context.EmploymentContracts.CountAsync(c => c.ContractNumber.StartsWith(seqPattern), cancellationToken);
        var contractNumber = $"{seqPattern}{(countThisYear + 1):D4}";

        var renewedContract = new EmploymentContract
        {
            ContractNumber = contractNumber,
            Title = "HỢP ĐỒNG LAO ĐỘNG (GIA HẠN / TÁI KÝ)",
            ContractType = newType,
            Status = EmploymentContractStatus.Draft,
            ContractDate = DateTime.UtcNow,
            ContractPlace = parent.ContractPlace,
            StartDate = dto.StartDate,
            EndDate = newType == EmploymentContractType.IndefiniteTerm ? null : dto.EndDate,
            ParentContractId = parent.Id,
            RenewalCount = parent.RenewalCount + 1,

            EmployerName = parent.EmployerName,
            EmployerTaxCode = parent.EmployerTaxCode,
            EmployerAddress = parent.EmployerAddress,
            EmployerPhone = parent.EmployerPhone,
            EmployerRepresentativeName = parent.EmployerRepresentativeName,
            EmployerRepresentativePosition = parent.EmployerRepresentativePosition,
            EmployerRepresentativeAuthority = parent.EmployerRepresentativeAuthority,

            EmployeeId = parent.EmployeeId,
            EmployeeCode = parent.EmployeeCode,
            EmployeeFullName = parent.EmployeeFullName,
            EmployeeDateOfBirth = parent.EmployeeDateOfBirth,
            EmployeeGender = parent.EmployeeGender,
            EmployeeNationalId = parent.EmployeeNationalId,
            EmployeeAddress = parent.EmployeeAddress,
            EmployeePhone = parent.EmployeePhone,
            EmployeeEmail = parent.EmployeeEmail,
            EmployeeBankInformation = parent.EmployeeBankInformation,

            JobTitle = parent.JobTitle,
            DepartmentName = parent.DepartmentName,
            Workplace = parent.Workplace,
            JobDescription = parent.JobDescription,
            WorkingHoursPerWeek = parent.WorkingHoursPerWeek,
            WorkingSchedule = parent.WorkingSchedule,
            AnnualLeaveDays = parent.AnnualLeaveDays,

            BasicSalary = dto.BasicSalary.HasValue && dto.BasicSalary.Value > 0 ? dto.BasicSalary : parent.BasicSalary,
            SalaryClauseText = dto.SalaryClauseText ?? parent.SalaryClauseText,
            SalaryType = parent.SalaryType,
            PaymentPeriod = parent.PaymentPeriod,
            PaymentMethod = parent.PaymentMethod,
            PositionAllowance = dto.PositionAllowance.HasValue && dto.PositionAllowance.Value > 0 ? dto.PositionAllowance : parent.PositionAllowance,
            LunchAllowance = dto.LunchAllowance.HasValue && dto.LunchAllowance.Value > 0 ? dto.LunchAllowance : parent.LunchAllowance,
            OtherAllowances = dto.OtherAllowances.HasValue && dto.OtherAllowances.Value > 0 ? dto.OtherAllowances : parent.OtherAllowances,
            SalaryReviewPolicy = parent.SalaryReviewPolicy,
            SocialInsuranceRate = parent.SocialInsuranceRate,
            LaborProtection = parent.LaborProtection,

            TrainingSupervisor = parent.TrainingSupervisor,
            TrainingObjectives = parent.TrainingObjectives,
            InternshipProgramme = parent.InternshipProgramme,

            HasNonDisclosureAgreement = parent.HasNonDisclosureAgreement,
            NdaClauseDetails = parent.NdaClauseDetails,
            HasTrainingReimbursement = parent.HasTrainingReimbursement,
            TrainingClauseDetails = parent.TrainingClauseDetails,
            HasSpecialBenefits = parent.HasSpecialBenefits,
            SpecialBenefitsDetails = parent.SpecialBenefitsDetails,
            CustomTerms = parent.CustomTerms,
            CustomClausesJson = parent.CustomClausesJson,

            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userName
        };

        _context.EmploymentContracts.Add(renewedContract);

        // Mark old contract as expired/superseded if it was active
        if (parent.Status == EmploymentContractStatus.Active || parent.Status == EmploymentContractStatus.ExpiringSoon)
        {
            parent.Status = EmploymentContractStatus.Expired;
            parent.UpdatedAt = DateTime.UtcNow;
            parent.UpdatedBy = userName;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractRenewed,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: renewedContract.Id.ToString(),
            details: $"Gia hạn / tái ký hợp đồng mới số {renewedContract.ContractNumber} từ hợp đồng gốc số {parent.ContractNumber} (Lần gia hạn {renewedContract.RenewalCount})",
            cancellationToken: cancellationToken);

        return MapToDto(renewedContract, true);
    }

    public async Task<bool> TerminateContractAsync(
        int id,
        DateTime terminationDate,
        string reason,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.EmploymentContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (contract == null) return false;

        contract.Status = EmploymentContractStatus.Terminated;
        contract.TerminationDate = terminationDate;
        contract.TerminationReason = reason;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = userName;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractTerminated,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: contract.Id.ToString(),
            details: $"Chấm dứt hợp đồng lao động số {contract.ContractNumber} vào ngày {terminationDate:dd/MM/yyyy}. Lý do: {reason}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> CancelContractAsync(
        int id,
        string reason,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.EmploymentContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (contract == null) return false;

        if (contract.Status == EmploymentContractStatus.Active)
            throw new InvalidOperationException("Hợp đồng đang có hiệu lực không thể hủy trực tiếp, phải thực hiện thủ tục Chấm dứt hợp đồng.");

        contract.Status = EmploymentContractStatus.Cancelled;
        contract.CancellationReason = reason;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = userName;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractCancelled,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: contract.Id.ToString(),
            details: $"Hủy hợp đồng lao động số {contract.ContractNumber}. Lý do: {reason}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteContractAsync(
        int id,
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.EmploymentContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (contract == null) return false;

        if (contract.Status != EmploymentContractStatus.Draft && contract.Status != EmploymentContractStatus.Cancelled)
            throw new InvalidOperationException("Chỉ được xóa các bản ghi hợp đồng ở trạng thái Bản nháp hoặc Đã hủy.");

        _context.EmploymentContracts.Remove(contract);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            eventType: AuditEventType.EmploymentContractDeleted,
            performedBy: userName,
            targetEntity: nameof(EmploymentContract),
            targetId: id.ToString(),
            details: $"Xóa hợp đồng lao động số {contract.ContractNumber}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<byte[]> GeneratePdfAsync(int id, CancellationToken cancellationToken = default)
    {
        var contract = await _context.EmploymentContracts
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new InvalidOperationException($"Không tìm thấy hợp đồng ID {id}");

        var dto = MapToDto(contract, true);
        return GeneratePdfFromDto(dto);
    }

    public byte[] GeneratePdfFromDto(EmploymentContractDto dto)
    {
        var document = new EmploymentContractPdfDocument(dto);
        return document.GeneratePdf();
    }

    public async Task<EmploymentContractStatsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _context.EmploymentContracts.AsNoTracking().ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var stats = new EmploymentContractStatsDto
        {
            TotalCount = all.Count,
            DraftCount = all.Count(c => c.Status == EmploymentContractStatus.Draft),
            PendingSignatureCount = all.Count(c => c.Status == EmploymentContractStatus.PendingSignature),
            ActiveCount = all.Count(c => c.Status == EmploymentContractStatus.Active && (!c.EndDate.HasValue || c.EndDate.Value > now.Date.AddDays(30))),
            ExpiringSoonCount = all.Count(c => c.Status == EmploymentContractStatus.Active && c.EndDate.HasValue && c.EndDate.Value <= now.Date.AddDays(30) && c.EndDate.Value >= now.Date),
            ExpiredCount = all.Count(c => c.Status == EmploymentContractStatus.Expired || (c.Status == EmploymentContractStatus.Active && c.EndDate.HasValue && c.EndDate.Value < now.Date)),
            TerminatedCount = all.Count(c => c.Status == EmploymentContractStatus.Terminated),
            IndefiniteTermCount = all.Count(c => c.ContractType == EmploymentContractType.IndefiniteTerm),
            FixedTermCount = all.Count(c => c.ContractType == EmploymentContractType.FixedTerm),
            ProbationCount = all.Count(c => c.ContractType == EmploymentContractType.Probation)
        };

        return stats;
    }

    private static EmploymentContractDto MapToDto(EmploymentContract entity, bool canViewSensitiveData)
    {
        var dto = new EmploymentContractDto
        {
            Id = entity.Id,
            ContractNumber = entity.ContractNumber,
            Title = entity.Title,
            ContractType = entity.ContractType,
            Status = entity.Status,
            ContractDate = entity.ContractDate,
            ContractPlace = entity.ContractPlace,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ProbationDurationDays = entity.ProbationDurationDays,
            ProbationSalaryPercentage = entity.ProbationSalaryPercentage,

            EmployerName = entity.EmployerName,
            EmployerTaxCode = entity.EmployerTaxCode,
            EmployerAddress = entity.EmployerAddress,
            EmployerPhone = entity.EmployerPhone,
            EmployerRepresentativeName = entity.EmployerRepresentativeName,
            EmployerRepresentativePosition = entity.EmployerRepresentativePosition,
            EmployerRepresentativeAuthority = entity.EmployerRepresentativeAuthority,

            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.EmployeeCode,
            EmployeeFullName = entity.EmployeeFullName,
            EmployeeDateOfBirth = entity.EmployeeDateOfBirth,
            EmployeeGender = entity.EmployeeGender,
            EmployeeNationalId = canViewSensitiveData ? entity.EmployeeNationalId : MaskSensitiveString(entity.EmployeeNationalId),
            EmployeeNationalIdIssueDate = entity.EmployeeNationalIdIssueDate,
            EmployeeNationalIdIssuePlace = entity.EmployeeNationalIdIssuePlace,
            EmployeeAddress = entity.EmployeeAddress,
            EmployeePhone = entity.EmployeePhone,
            EmployeeEmail = entity.EmployeeEmail,
            EmployeeBankInformation = canViewSensitiveData ? entity.EmployeeBankInformation : MaskSensitiveString(entity.EmployeeBankInformation),

            JobTitle = entity.JobTitle,
            DepartmentName = entity.DepartmentName,
            Workplace = entity.Workplace,
            JobDescription = entity.JobDescription,
            WorkingHoursPerWeek = entity.WorkingHoursPerWeek,
            WorkingSchedule = entity.WorkingSchedule,
            AnnualLeaveDays = entity.AnnualLeaveDays,

            BasicSalary = canViewSensitiveData ? entity.BasicSalary : null,
            SalaryClauseText = entity.SalaryClauseText,
            SalaryType = entity.SalaryType,
            PaymentPeriod = entity.PaymentPeriod,
            PaymentMethod = entity.PaymentMethod,
            PositionAllowance = canViewSensitiveData ? entity.PositionAllowance : null,
            LunchAllowance = canViewSensitiveData ? entity.LunchAllowance : null,
            OtherAllowances = canViewSensitiveData ? entity.OtherAllowances : null,
            SalaryReviewPolicy = entity.SalaryReviewPolicy,
            SocialInsuranceRate = entity.SocialInsuranceRate,
            LaborProtection = entity.LaborProtection,

            TrainingSupervisor = entity.TrainingSupervisor,
            TrainingObjectives = entity.TrainingObjectives,
            InternshipProgramme = entity.InternshipProgramme,

            HasNonDisclosureAgreement = entity.HasNonDisclosureAgreement,
            NdaClauseDetails = entity.NdaClauseDetails,
            HasTrainingReimbursement = entity.HasTrainingReimbursement,
            TrainingClauseDetails = entity.TrainingClauseDetails,
            HasSpecialBenefits = entity.HasSpecialBenefits,
            SpecialBenefitsDetails = entity.SpecialBenefitsDetails,
            CustomTerms = entity.CustomTerms,
            CustomClausesJson = entity.CustomClausesJson,

            SigningMethod = entity.SigningMethod,
            EmployeeSignedAt = entity.EmployeeSignedAt,
            EmployeeSignatureData = entity.EmployeeSignatureData,
            EmployerSignedAt = entity.EmployerSignedAt,
            EmployerSignatureData = entity.EmployerSignatureData,
            DigitalSignatureHash = entity.DigitalSignatureHash,
            NationalPlatformReference = entity.NationalPlatformReference,
            PaperScanAttachmentPath = entity.PaperScanAttachmentPath,

            ParentContractId = entity.ParentContractId,
            RenewalCount = entity.RenewalCount,
            TerminationDate = entity.TerminationDate,
            TerminationReason = entity.TerminationReason,
            CancellationReason = entity.CancellationReason,
            Notes = entity.Notes,

            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy,
            CanViewSensitiveData = canViewSensitiveData
        };

        if (!string.IsNullOrWhiteSpace(entity.CustomClausesJson))
        {
            try
            {
                dto.Clauses = JsonSerializer.Deserialize<List<EmploymentContractClauseDto>>(entity.CustomClausesJson) ?? new();
            }
            catch
            {
                dto.Clauses = EmploymentContractClauseDefaults.GenerateDefaultClauses(dto);
            }
        }
        else
        {
            dto.Clauses = EmploymentContractClauseDefaults.GenerateDefaultClauses(dto);
        }

        return dto;
    }

    private static string? MaskSensitiveString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= 4) return "****";
        return string.Concat(new string('*', value.Length - 4), value.AsSpan(value.Length - 4));
    }
}
