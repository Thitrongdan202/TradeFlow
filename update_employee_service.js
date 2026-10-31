const fs = require('fs');

const path = 'src/TradeFlow.Infrastructure/Services/EmployeeService.cs';
let content = fs.readFileSync(path, 'utf8');

// 1. Update review status check
const checkTarget = `        if (request.Status != EmployeeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Yêu cầu này đã được xử lý trước đó (trạng thái: {request.Status}).");
        }`;

const checkReplacement = `        if (request.Status != EmployeeRequestStatus.Pending && request.Status != EmployeeRequestStatus.NeedAdditionalDocs)
        {
            throw new InvalidOperationException($"Yêu cầu này đã được xử lý trước đó (trạng thái: {request.Status}).");
        }`;

content = content.replace(checkTarget, checkReplacement);

// 2. Add ResubmitEmployeeRequestAsync before ReviewEmployeeRequestAsync
const resubmitMethod = `    public async Task<EmployeeRequestDto> ResubmitEmployeeRequestAsync(ResubmitEmployeeRequestCommand command, string performedBy, CancellationToken cancellationToken = default)
    {
        var request = await _context.EmployeeRequests
            .Include(r => r.Department)
            .Include(r => r.Position)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy yêu cầu thêm nhân viên ID '{command.RequestId}'.");
        }

        if (request.Status != EmployeeRequestStatus.NeedAdditionalDocs && request.Status != EmployeeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể gửi lại yêu cầu đang ở trạng thái 'Cần bổ sung hồ sơ' hoặc 'Chờ duyệt'. Trạng thái hiện tại: {request.Status}.");
        }

        if (!string.IsNullOrWhiteSpace(command.FullName)) request.FullName = command.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(command.Phone)) request.Phone = command.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(command.Email)) request.Email = command.Email.Trim();
        if (!string.IsNullOrWhiteSpace(command.NationalId)) request.NationalId = command.NationalId.Trim();
        if (command.DepartmentId.HasValue && command.DepartmentId.Value > 0) request.DepartmentId = command.DepartmentId.Value;
        if (command.PositionId.HasValue && command.PositionId.Value > 0) request.PositionId = command.PositionId.Value;
        if (command.DateOfBirth.HasValue) request.DateOfBirth = NormalizeToUtc(command.DateOfBirth);
        if (!string.IsNullOrWhiteSpace(command.Gender)) request.Gender = command.Gender.Trim();
        if (command.StartDate.HasValue) request.StartDate = NormalizeToUtc(command.StartDate);
        if (!string.IsNullOrWhiteSpace(command.WorkingBranch)) request.WorkingBranch = command.WorkingBranch.Trim();
        if (!string.IsNullOrWhiteSpace(command.PayrollBranch)) request.PayrollBranch = command.PayrollBranch.Trim();
        if (!string.IsNullOrWhiteSpace(command.BankInformation)) request.BankInformation = command.BankInformation.Trim();
        if (!string.IsNullOrWhiteSpace(command.Address)) request.Address = command.Address.Trim();
        if (!string.IsNullOrWhiteSpace(command.ProposedUserId)) request.ProposedUserId = command.ProposedUserId.Trim();

        if (!string.IsNullOrWhiteSpace(command.ResubmitNotes))
        {
            request.Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? $"[Gửi lại bổ sung {DateTime.Now:dd/MM/yyyy HH:mm}]: {command.ResubmitNotes.Trim()}"
                : $"{request.Notes}\\n[Gửi lại bổ sung {DateTime.Now:dd/MM/yyyy HH:mm}]: {command.ResubmitNotes.Trim()}";
        }

        request.Status = EmployeeRequestStatus.Pending; // Transitions back to Pending for review
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.EmployeeRequestUpdated,
            performedBy,
            "EmployeeRequest",
            request.Id.ToString(),
            $"Bổ sung hồ sơ và gửi lại yêu cầu tuyển dụng/thêm nhân viên '{request.FullName}' để xem xét phê duyệt.");

        return new EmployeeRequestDto
        {
            Id = request.Id,
            Code = request.Code,
            FullName = request.FullName,
            NationalId = request.NationalId,
            Phone = request.Phone,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.Department?.Name,
            PositionId = request.PositionId,
            PositionName = request.Position?.Name,
            Status = request.Status,
            RequestedBy = request.RequestedBy,
            ReviewNotes = request.ReviewNotes,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

`;

const reviewMethodTarget = `    public async Task<EmployeeDto?> ReviewEmployeeRequestAsync(ReviewEmployeeRequestCommand command, string reviewedBy, CancellationToken cancellationToken = default)`;

content = content.replace(reviewMethodTarget, resubmitMethod + reviewMethodTarget);

fs.writeFileSync(path, content, 'utf8');
console.log('Updated EmployeeService.cs with ResubmitEmployeeRequestAsync and review status check');
