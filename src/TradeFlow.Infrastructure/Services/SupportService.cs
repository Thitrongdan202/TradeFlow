using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Support;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Entities.Support;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class SupportService : ISupportService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif",
        ".pdf", ".docx", ".xlsx", ".txt", ".log", ".csv"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int MaxFilesCount = 5;

    private readonly IDbContextFactory<TradeFlowDbContext> _contextFactory;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<SupportService> _logger;

    public SupportService(
        IDbContextFactory<TradeFlowDbContext> contextFactory,
        IFileStorageService fileStorage,
        ILogger<SupportService> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SupportTicketDetailDto> CreateTicketAsync(CreateSupportTicketCommand command, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (string.IsNullOrWhiteSpace(command.Title)) throw new ArgumentException("Tiêu đề báo cáo không được để trống.", nameof(command.Title));
        if (string.IsNullOrWhiteSpace(command.Description)) throw new ArgumentException("Nội dung mô tả không được để trống.", nameof(command.Description));

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "system";
        string userName = user.Identity?.Name ?? "system";

        // Query submitter user and employee profile
        var appUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId || u.UserName == userName, ct);
        string submitterFullName = appUser?.FullName ?? userName;
        string? submitterDept = null;
        int? employeeId = appUser?.EmployeeId;

        if (employeeId.HasValue)
        {
            var emp = await context.Employees.AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId.Value, ct);
            if (emp != null)
            {
                submitterFullName = emp.FullName;
                submitterDept = emp.Department?.Name;
            }
        }

        string submitterRole = "Employee";
        if (user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase))
            submitterRole = "Administrator";
        else if (user.IsInRole("Manager"))
            submitterRole = "Manager";
        else if (user.IsInRole("HR"))
            submitterRole = "HR";
        else if (user.IsInRole("Sales"))
            submitterRole = "Sales";
        else if (user.IsInRole("Warehouse"))
            submitterRole = "Warehouse";
        else if (user.IsInRole("Accountant"))
            submitterRole = "Accountant";

        // Generate Code: SUP-YYYY-000001
        string code = await GenerateTicketCodeAsync(context, ct);

        var ticket = new SupportTicket
        {
            Code = code,
            Category = command.Category,
            Title = command.Title.Trim(),
            RelatedModule = command.RelatedModule,
            Description = command.Description.Trim(),
            Priority = command.Priority,
            Status = SupportStatus.New,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            SubmitterFullName = submitterFullName,
            SubmitterRole = submitterRole,
            SubmitterDepartment = submitterDept,
            EmployeeId = employeeId,
            CurrentRoute = command.CurrentRoute,
            AppVersion = "TradeFlow v2.4 Enterprise",
            CreatedAt = DateTime.UtcNow
        };

        // Attachments
        if (command.Files != null && command.Files.Count > 0)
        {
            int filesCount = Math.Min(command.Files.Count, MaxFilesCount);
            for (int i = 0; i < filesCount; i++)
            {
                var file = command.Files[i];
                if (file.Data == null || file.Data.Length == 0) continue;
                if (file.Data.Length > MaxFileSizeBytes) continue;

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext)) continue;

                using var stream = new MemoryStream(file.Data);
                string storagePath = await _fileStorage.SaveFileAsync(stream, file.FileName, file.ContentType, "support", ct);

                ticket.Attachments.Add(new SupportAttachment
                {
                    OriginalFileName = file.FileName,
                    StoragePath = storagePath,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Data.Length,
                    UploadedByUserId = userId,
                    UploadedByUserName = userName,
                    UploadedAt = DateTime.UtcNow
                });
            }
        }

        // Audit Log
        ticket.AuditLogs.Add(new SupportAuditLog
        {
            Action = "Created",
            PerformedByUserId = userId,
            PerformedByUserName = userName,
            NewValue = ticket.Status.ToDisplayName(),
            Notes = $"Tạo mới yêu cầu hỗ trợ {ticket.Code} ({ticket.Category.ToDisplayName()})",
            CreatedAt = DateTime.UtcNow
        });

        context.SupportTickets.Add(ticket);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Support ticket created successfully: {TicketCode} by {UserName}", ticket.Code, userName);

        return await GetTicketByIdInternalAsync(context, ticket.Id, user, ct)
               ?? throw new InvalidOperationException("Không thể tải thông tin báo cáo vừa tạo.");
    }

    public async Task<List<SupportTicketSummaryDto>> GetTicketsAsync(SupportTicketFilter filter, ClaimsPrincipal user, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "";
        string userName = user.Identity?.Name ?? "";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");

        // Determine manager's department
        string? managerDept = null;
        if (isManager && !isAdmin)
        {
            var appUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId || u.UserName == userName, ct);
            if (appUser?.EmployeeId != null)
            {
                var emp = await context.Employees.AsNoTracking()
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == appUser.EmployeeId.Value, ct);
                managerDept = emp?.Department?.Name;
            }
        }

        var query = context.SupportTickets.AsNoTracking();

        // RBAC Scope
        if (!isAdmin)
        {
            if (isManager)
            {
                // Manager can see: their own tickets OR tickets assigned to them OR unassigned new tickets OR tickets from their department
                query = query.Where(t => t.CreatedByUserId == userId
                                      || t.AssignedToUserId == userId
                                      || t.AssignedToUserId == null
                                      || (managerDept != null && t.SubmitterDepartment == managerDept));
            }
            else
            {
                // Standard staff: only see their own tickets
                query = query.Where(t => t.CreatedByUserId == userId);
            }
        }

        // User requested "OnlyMine"
        if (filter.OnlyMine)
        {
            query = query.Where(t => t.CreatedByUserId == userId);
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
        {
            string term = filter.SearchKeyword.Trim().ToLower();
            query = query.Where(t => t.Code.ToLower().Contains(term)
                                  || t.Title.ToLower().Contains(term)
                                  || (t.SubmitterFullName != null && t.SubmitterFullName.ToLower().Contains(term))
                                  || t.CreatedByUserName.ToLower().Contains(term));
        }

        if (filter.Category.HasValue)
            query = query.Where(t => t.Category == filter.Category.Value);

        if (filter.RelatedModule.HasValue)
            query = query.Where(t => t.RelatedModule == filter.RelatedModule.Value);

        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.Department))
            query = query.Where(t => t.SubmitterDepartment == filter.Department);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.AssignedToUserId))
        {
            if (filter.AssignedToUserId == "unassigned")
                query = query.Where(t => t.AssignedToUserId == null);
            else
                query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.CreatedByUserId))
            query = query.Where(t => t.CreatedByUserId == filter.CreatedByUserId);

        var orderedQuery = filter.SortBy switch
        {
            "created_asc" => query.OrderBy(t => t.CreatedAt),
            "priority_desc" => query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var list = await orderedQuery
            .Select(t => new SupportTicketSummaryDto
            {
                Id = t.Id,
                Code = t.Code,
                Category = t.Category,
                Title = t.Title,
                RelatedModule = t.RelatedModule,
                Priority = t.Priority,
                Status = t.Status,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName = t.CreatedByUserName,
                SubmitterFullName = t.SubmitterFullName,
                SubmitterRole = t.SubmitterRole,
                SubmitterDepartment = t.SubmitterDepartment,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                AttachmentCount = t.Attachments.Count,
                CommentCount = t.Comments.Count,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToUserName = t.AssignedToUserName,
                AssignedToFullName = t.AssignedToFullName
            })
            .ToListAsync(ct);

        return list;
    }

    public async Task<SupportTicketDetailDto?> GetTicketByIdAsync(int id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await GetTicketByIdInternalAsync(context, id, user, ct);
    }

    public async Task<SupportTicketDetailDto?> GetTicketByCodeAsync(string code, ClaimsPrincipal user, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var ticket = await context.SupportTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Code == code, ct);
        if (ticket == null) return null;
        return await GetTicketByIdInternalAsync(context, ticket.Id, user, ct);
    }

    private async Task<SupportTicketDetailDto?> GetTicketByIdInternalAsync(TradeFlowDbContext context, int id, ClaimsPrincipal user, CancellationToken ct)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "";
        string userName = user.Identity?.Name ?? "";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");

        var ticket = await context.SupportTickets
            .AsNoTracking()
            .Include(t => t.Attachments)
            .Include(t => t.Comments)
                .ThenInclude(c => c.Attachments)
            .Include(t => t.AuditLogs)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (ticket == null) return null;

        // RBAC access check
        if (!isAdmin)
        {
            if (isManager)
            {
                // Can view if created by self, assigned to self, unassigned (new), or in department
                if (ticket.CreatedByUserId != userId && ticket.AssignedToUserId != userId && ticket.AssignedToUserId != null)
                {
                    // Check department
                    var appUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId || u.UserName == userName, ct);
                    if (appUser?.EmployeeId != null)
                    {
                        var emp = await context.Employees.AsNoTracking().Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == appUser.EmployeeId.Value, ct);
                        if (emp?.Department?.Name != ticket.SubmitterDepartment)
                            return null;
                    }
                    else return null;
                }
            }
            else
            {
                // Regular staff: only own ticket
                if (ticket.CreatedByUserId != userId) return null;
            }
        }

        // Hide internal notes from standard employee
        var visibleComments = ticket.Comments
            .Where(c => !c.IsInternalNote || isAdmin || isManager)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new SupportCommentDto
            {
                Id = c.Id,
                TicketId = c.TicketId,
                AuthorUserId = c.AuthorUserId,
                AuthorUserName = c.AuthorUserName,
                AuthorFullName = c.AuthorFullName,
                AuthorRole = c.AuthorRole,
                Content = c.Content,
                IsInternalNote = c.IsInternalNote,
                CreatedAt = c.CreatedAt,
                Attachments = c.Attachments.Select(a => new SupportAttachmentDto
                {
                    Id = a.Id,
                    TicketId = a.TicketId,
                    CommentId = a.CommentId,
                    OriginalFileName = a.OriginalFileName,
                    StoragePath = a.StoragePath,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedByUserName = a.UploadedByUserName,
                    UploadedAt = a.UploadedAt
                }).ToList()
            }).ToList();

        var dto = new SupportTicketDetailDto
        {
            Id = ticket.Id,
            Code = ticket.Code,
            Category = ticket.Category,
            Title = ticket.Title,
            RelatedModule = ticket.RelatedModule,
            Description = ticket.Description,
            Priority = ticket.Priority,
            Status = ticket.Status,
            RejectionReason = ticket.RejectionReason,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = ticket.CreatedByUserName,
            SubmitterFullName = ticket.SubmitterFullName,
            SubmitterRole = ticket.SubmitterRole,
            SubmitterDepartment = ticket.SubmitterDepartment,
            CurrentRoute = ticket.CurrentRoute,
            AppVersion = ticket.AppVersion,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedToUserName = ticket.AssignedToUserName,
            AssignedToFullName = ticket.AssignedToFullName,
            AttachmentCount = ticket.Attachments.Count,
            CommentCount = visibleComments.Count,
            Attachments = ticket.Attachments.Select(a => new SupportAttachmentDto
            {
                Id = a.Id,
                TicketId = a.TicketId,
                CommentId = a.CommentId,
                OriginalFileName = a.OriginalFileName,
                StoragePath = a.StoragePath,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                UploadedByUserId = a.UploadedByUserId,
                UploadedByUserName = a.UploadedByUserName,
                UploadedAt = a.UploadedAt
            }).ToList(),
            Comments = visibleComments,
            AuditLogs = ticket.AuditLogs.OrderByDescending(al => al.CreatedAt).Select(al => new SupportAuditLogDto
            {
                Id = al.Id,
                Action = al.Action,
                PerformedByUserName = al.PerformedByUserName,
                OldValue = al.OldValue,
                NewValue = al.NewValue,
                Notes = al.Notes,
                CreatedAt = al.CreatedAt
            }).ToList()
        };

        return dto;
    }

    public async Task<SupportCommentDto> AddCommentAsync(AddSupportCommentCommand command, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (string.IsNullOrWhiteSpace(command.Content)) throw new ArgumentException("Nội dung phản hồi không được để trống.", nameof(command.Content));

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var ticket = await context.SupportTickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == command.TicketId, ct);

        if (ticket == null) throw new KeyNotFoundException($"Không tìm thấy yêu cầu hỗ trợ với ID {command.TicketId}");

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "system";
        string userName = user.Identity?.Name ?? "system";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");

        // Standard staff cannot add internal notes
        bool isInternal = command.IsInternalNote && (isAdmin || isManager);

        var appUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId || u.UserName == userName, ct);
        string authorFullName = appUser?.FullName ?? userName;
        string authorRole = isAdmin ? "Administrator" : (isManager ? "Manager" : "Nhân viên");

        var comment = new SupportComment
        {
            TicketId = ticket.Id,
            AuthorUserId = userId,
            AuthorUserName = userName,
            AuthorFullName = authorFullName,
            AuthorRole = authorRole,
            Content = command.Content.Trim(),
            IsInternalNote = isInternal,
            CreatedAt = DateTime.UtcNow
        };

        // Attachments
        if (command.Files != null && command.Files.Count > 0)
        {
            foreach (var file in command.Files.Take(MaxFilesCount))
            {
                if (file.Data == null || file.Data.Length == 0) continue;
                if (file.Data.Length > MaxFileSizeBytes) continue;

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext)) continue;

                using var stream = new MemoryStream(file.Data);
                string storagePath = await _fileStorage.SaveFileAsync(stream, file.FileName, file.ContentType, "support", ct);

                comment.Attachments.Add(new SupportAttachment
                {
                    TicketId = ticket.Id,
                    OriginalFileName = file.FileName,
                    StoragePath = storagePath,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Data.Length,
                    UploadedByUserId = userId,
                    UploadedByUserName = userName,
                    UploadedAt = DateTime.UtcNow
                });
            }
        }

        ticket.Comments.Add(comment);
        ticket.UpdatedAt = DateTime.UtcNow;

        // Auto status transition
        if (!isInternal)
        {
            bool isSubmitter = string.Equals(ticket.CreatedByUserId, userId, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(ticket.CreatedByUserName, userName, StringComparison.OrdinalIgnoreCase);

            if (isSubmitter && ticket.Status == SupportStatus.WaitingForCustomer)
            {
                var oldStatus = ticket.Status;
                ticket.Status = SupportStatus.InProgress;
                ticket.AuditLogs.Add(new SupportAuditLog
                {
                    TicketId = ticket.Id,
                    Action = "StatusChanged",
                    PerformedByUserId = userId,
                    PerformedByUserName = userName,
                    OldValue = oldStatus.ToDisplayName(),
                    NewValue = ticket.Status.ToDisplayName(),
                    Notes = "Người gửi đã phản hồi, tự động chuyển trạng thái về Đang xử lý.",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        ticket.AuditLogs.Add(new SupportAuditLog
        {
            TicketId = ticket.Id,
            Action = isInternal ? "InternalNote" : "Replied",
            PerformedByUserId = userId,
            PerformedByUserName = userName,
            Notes = isInternal ? "Thêm ghi chú nội bộ" : "Gửi phản hồi công khai",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);

        return new SupportCommentDto
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            AuthorUserId = comment.AuthorUserId,
            AuthorUserName = comment.AuthorUserName,
            AuthorFullName = comment.AuthorFullName,
            AuthorRole = comment.AuthorRole,
            Content = comment.Content,
            IsInternalNote = comment.IsInternalNote,
            CreatedAt = comment.CreatedAt,
            Attachments = comment.Attachments.Select(a => new SupportAttachmentDto
            {
                Id = a.Id,
                TicketId = a.TicketId,
                CommentId = a.CommentId,
                OriginalFileName = a.OriginalFileName,
                StoragePath = a.StoragePath,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                UploadedByUserId = a.UploadedByUserId,
                UploadedByUserName = a.UploadedByUserName,
                UploadedAt = a.UploadedAt
            }).ToList()
        };
    }

    public async Task<bool> UpdateTicketStatusAsync(UpdateSupportTicketStatusCommand command, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == command.TicketId, ct);
        if (ticket == null) return false;

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "system";
        string userName = user.Identity?.Name ?? "system";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");
        bool isSubmitter = string.Equals(ticket.CreatedByUserId, userId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(ticket.CreatedByUserName, userName, StringComparison.OrdinalIgnoreCase);

        // Permissions: Submitter can only resolve or close; Manager/Admin can change any status
        if (!isAdmin && !isManager)
        {
            if (!isSubmitter || (command.NewStatus != SupportStatus.Resolved && command.NewStatus != SupportStatus.Closed))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thay đổi trạng thái yêu cầu này.");
            }
        }

        var oldStatus = ticket.Status;
        if (oldStatus == command.NewStatus) return true;

        ticket.Status = command.NewStatus;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (command.NewStatus == SupportStatus.Resolved)
            ticket.ResolvedAt = DateTime.UtcNow;
        else if (command.NewStatus == SupportStatus.Closed)
            ticket.ClosedAt = DateTime.UtcNow;
        else if (command.NewStatus == SupportStatus.Rejected)
            ticket.RejectionReason = command.ReasonOrNotes;

        context.SupportAuditLogs.Add(new SupportAuditLog
        {
            TicketId = ticket.Id,
            Action = "StatusChanged",
            PerformedByUserId = userId,
            PerformedByUserName = userName,
            OldValue = oldStatus.ToDisplayName(),
            NewValue = command.NewStatus.ToDisplayName(),
            Notes = command.ReasonOrNotes ?? $"Cập nhật trạng thái sang {command.NewStatus.ToDisplayName()}",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AssignTicketAsync(AssignSupportTicketCommand command, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "system";
        string userName = user.Identity?.Name ?? "system";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");

        if (!isAdmin && !isManager)
            throw new UnauthorizedAccessException("Chỉ Quản lý hoặc Quản trị viên mới có quyền phân công xử lý yêu cầu.");

        var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == command.TicketId, ct);
        if (ticket == null) return false;

        var assignedUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.AssignToUserId, ct);
        if (assignedUser == null) throw new KeyNotFoundException("Không tìm thấy người được phân công.");

        string oldAssignee = ticket.AssignedToFullName ?? ticket.AssignedToUserName ?? "Chưa phân công";

        ticket.AssignedToUserId = assignedUser.Id;
        ticket.AssignedToUserName = assignedUser.UserName;
        ticket.AssignedToFullName = assignedUser.FullName ?? assignedUser.UserName;
        ticket.UpdatedAt = DateTime.UtcNow;

        // If ticket was New, transition to Acknowledged upon assignment
        if (ticket.Status == SupportStatus.New)
        {
            ticket.Status = SupportStatus.Acknowledged;
        }

        context.SupportAuditLogs.Add(new SupportAuditLog
        {
            TicketId = ticket.Id,
            Action = "Assigned",
            PerformedByUserId = userId,
            PerformedByUserName = userName,
            OldValue = oldAssignee,
            NewValue = ticket.AssignedToFullName,
            Notes = command.Notes ?? $"Phân công cho {ticket.AssignedToFullName}",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SupportStatsDto> GetStatsAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "";
        string userName = user.Identity?.Name ?? "";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");

        string? managerDept = null;
        if (isManager && !isAdmin)
        {
            var appUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId || u.UserName == userName, ct);
            if (appUser?.EmployeeId != null)
            {
                var emp = await context.Employees.AsNoTracking().Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == appUser.EmployeeId.Value, ct);
                managerDept = emp?.Department?.Name;
            }
        }

        var query = context.SupportTickets.AsNoTracking();

        if (!isAdmin)
        {
            if (isManager)
            {
                query = query.Where(t => t.CreatedByUserId == userId
                                      || t.AssignedToUserId == userId
                                      || (managerDept != null && t.SubmitterDepartment == managerDept));
            }
            else
            {
                query = query.Where(t => t.CreatedByUserId == userId);
            }
        }

        var list = await query.Select(t => t.Status).ToListAsync(ct);

        return new SupportStatsDto
        {
            TotalTickets = list.Count,
            NewTickets = list.Count(s => s == SupportStatus.New),
            InProgressTickets = list.Count(s => s == SupportStatus.InProgress || s == SupportStatus.Acknowledged),
            WaitingCustomerTickets = list.Count(s => s == SupportStatus.WaitingForCustomer),
            ResolvedTickets = list.Count(s => s == SupportStatus.Resolved),
            ClosedTickets = list.Count(s => s == SupportStatus.Closed),
            RejectedTickets = list.Count(s => s == SupportStatus.Rejected)
        };
    }

    public async Task<int> GetBadgeCountAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (user.Identity?.IsAuthenticated != true) return 0;

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "";
        string userName = user.Identity?.Name ?? "";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");

        if (isAdmin)
        {
            // Admin: count tickets that are New or Acknowledged without assignee
            return await context.SupportTickets.AsNoTracking()
                .CountAsync(t => t.Status == SupportStatus.New, ct);
        }

        if (isManager)
        {
            // Manager: count new tickets in their department or assigned to them needing review
            var appUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId || u.UserName == userName, ct);
            string? dept = null;
            if (appUser?.EmployeeId != null)
            {
                var emp = await context.Employees.AsNoTracking().Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == appUser.EmployeeId.Value, ct);
                dept = emp?.Department?.Name;
            }

            return await context.SupportTickets.AsNoTracking()
                .CountAsync(t => (t.Status == SupportStatus.New && dept != null && t.SubmitterDepartment == dept)
                              || (t.AssignedToUserId == userId && t.Status == SupportStatus.New), ct);
        }

        // Standard staff: count tickets that are WaitingForCustomer or recently Resolved
        return await context.SupportTickets.AsNoTracking()
            .CountAsync(t => t.CreatedByUserId == userId && t.Status == SupportStatus.WaitingForCustomer, ct);
    }

    public async Task<(SupportAttachmentDto? Attachment, Stream? Stream)> GetAttachmentStreamAsync(int attachmentId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var att = await context.SupportAttachments
            .Include(a => a.Ticket)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        if (att == null) return (null, null);

        // RBAC check on Ticket
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "";
        string userName = user.Identity?.Name ?? "";
        bool isAdmin = user.IsInRole("Administrator") || userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
        bool isManager = user.IsInRole("Manager");

        if (!isAdmin)
        {
            if (att.Ticket != null)
            {
                if (!isManager && att.Ticket.CreatedByUserId != userId)
                {
                    return (null, null); // Unauthorized
                }
            }
        }

        var stream = await _fileStorage.GetFileAsync(att.StoragePath, ct);
        var dto = new SupportAttachmentDto
        {
            Id = att.Id,
            TicketId = att.TicketId,
            CommentId = att.CommentId,
            OriginalFileName = att.OriginalFileName,
            StoragePath = att.StoragePath,
            ContentType = att.ContentType,
            FileSizeBytes = att.FileSizeBytes,
            UploadedByUserId = att.UploadedByUserId,
            UploadedByUserName = att.UploadedByUserName,
            UploadedAt = att.UploadedAt
        };

        return (dto, stream);
    }

    private static async Task<string> GenerateTicketCodeAsync(TradeFlowDbContext context, CancellationToken ct)
    {
        var seq = await context.SystemSequences.FirstOrDefaultAsync(s => s.SequenceKey == "SupportTicket", ct);
        if (seq == null)
        {
            seq = new SystemSequence
            {
                SequenceKey = "SupportTicket",
                Prefix = "SUP-",
                CurrentNumber = 1
            };
            context.SystemSequences.Add(seq);
        }

        string code = $"{seq.Prefix}{DateTime.UtcNow.Year}-{seq.CurrentNumber:D6}";
        seq.CurrentNumber++;
        return code;
    }
}
