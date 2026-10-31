const fs = require('fs');

const path = 'src/TradeFlow.Infrastructure/Services/NotificationService.cs';
let content = fs.readFileSync(path, 'utf8');

const pendingBlockTarget = `                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = $"Đề xuất nhân viên: {req.FullName}",
                        Message = $"Yêu cầu tuyển dụng/thêm nhân sự '{req.FullName}' ({posName}{deptName}) do {req.RequestedBy} đề xuất.",
                        Type = "approval",
                        CreatedAt = req.CreatedAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                        IsApprovalRequest = true,
                        RequestType = "EmployeeRequest",
                        RequestTypeName = "Đề xuất thêm nhân sự",
                        EntityId = req.Id,
                        RequestCode = codeDisplay,
                        Requester = req.RequestedBy,
                        Status = "Chờ duyệt",
                        CanApprove = canApprove,
                        IsSelfRequest = isSelf
                    });
                }`;

const pendingBlockReplacement = `                    bool isResubmitted = req.UpdatedAt.HasValue && req.UpdatedAt.Value > req.CreatedAt.AddSeconds(5);
                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = isResubmitted ? $"Hồ sơ nhân sự gửi lại chờ duyệt: {req.FullName}" : $"Đề xuất nhân viên: {req.FullName}",
                        Message = isResubmitted
                            ? $"Yêu cầu nhân sự '{req.FullName}' ({posName}{deptName}) đã được cập nhật/bổ sung hồ sơ và gửi lại để phê duyệt."
                            : $"Yêu cầu tuyển dụng/thêm nhân sự '{req.FullName}' ({posName}{deptName}) do {req.RequestedBy} đề xuất.",
                        Type = "approval",
                        CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                        ActionButtonText = "Xem hồ sơ chờ duyệt",
                        IsApprovalRequest = true,
                        RequestType = "EmployeeRequest",
                        RequestTypeName = isResubmitted ? "Gửi lại chờ duyệt" : "Đề xuất thêm nhân sự",
                        EntityId = req.Id,
                        RequestCode = codeDisplay,
                        Requester = req.RequestedBy,
                        Status = isResubmitted ? "Gửi lại chờ duyệt" : "Chờ duyệt",
                        CanApprove = canApprove,
                        IsSelfRequest = isSelf
                    });
                }

                // 1b. Employee Feedback Notifications (NeedAdditionalDocs, Rejected, Approved)
                var feedbackEmpRequests = await context.EmployeeRequests
                    .AsNoTracking()
                    .Include(r => r.Position)
                    .Include(r => r.Department)
                    .Where(r => (r.Status == EmployeeRequestStatus.NeedAdditionalDocs
                              || r.Status == EmployeeRequestStatus.Rejected
                              || r.Status == EmployeeRequestStatus.Approved)
                              && (string.IsNullOrEmpty(currentUserName) || r.RequestedBy == currentUserName || isAdmin))
                    .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var req in feedbackEmpRequests)
                {
                    string posName = req.Position?.Name ?? "Nhân viên";
                    string deptName = req.Department?.Name != null ? $" - {req.Department.Name}" : "";
                    string codeDisplay = !string.IsNullOrWhiteSpace(req.Code) ? req.Code : $"YC-{req.Id:D4}";

                    if (req.Status == EmployeeRequestStatus.NeedAdditionalDocs)
                    {
                        string id = $"emp_needdocs_{req.Id}_{req.UpdatedAt?.Ticks ?? req.CreatedAt.Ticks}";
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Yêu cầu bổ sung hồ sơ: {req.FullName}",
                            Message = $"Hồ sơ nhân sự '{req.FullName}' ({posName}{deptName}) cần bổ sung tài liệu. Ý kiến người duyệt: {req.ReviewNotes ?? "Vui lòng hoàn thiện hồ sơ"}",
                            Type = "warning",
                            CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                            ActionButtonText = "Mở yêu cầu bổ sung",
                            IsApprovalRequest = false,
                            RequestType = "EmployeeRequest",
                            RequestTypeName = "Cần bổ sung hồ sơ",
                            EntityId = req.Id,
                            RequestCode = codeDisplay,
                            Requester = req.RequestedBy,
                            Status = "Cần bổ sung hồ sơ"
                        });
                    }
                    else if (req.Status == EmployeeRequestStatus.Rejected)
                    {
                        string id = $"emp_rejected_{req.Id}_{req.UpdatedAt?.Ticks ?? req.CreatedAt.Ticks}";
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Hồ sơ nhân sự bị từ chối: {req.FullName}",
                            Message = $"Yêu cầu tuyển dụng/thêm nhân sự '{req.FullName}' ({posName}{deptName}) đã bị từ chối. Lý do: {req.ReviewNotes ?? "Không đáp ứng yêu cầu tuyển dụng"}",
                            Type = "error",
                            CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                            ActionButtonText = "Xem lý do từ chối",
                            IsApprovalRequest = false,
                            RequestType = "EmployeeRequest",
                            RequestTypeName = "Từ chối hồ sơ",
                            EntityId = req.Id,
                            RequestCode = codeDisplay,
                            Requester = req.RequestedBy,
                            Status = "Từ chối"
                        });
                    }
                    else if (req.Status == EmployeeRequestStatus.Approved)
                    {
                        string id = $"emp_approved_{req.Id}_{req.UpdatedAt?.Ticks ?? req.CreatedAt.Ticks}";
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Hồ sơ nhân sự đã duyệt: {req.FullName}",
                            Message = $"Hồ sơ nhân sự '{req.FullName}' ({posName}{deptName}) đã được phê duyệt thành công bởi {req.ApprovedBy}.",
                            Type = "success",
                            CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                            ActionButtonText = "Xem hồ sơ nhân viên",
                            IsApprovalRequest = false,
                            RequestType = "EmployeeRequest",
                            RequestTypeName = "Đã phê duyệt",
                            EntityId = req.Id,
                            RequestCode = codeDisplay,
                            Requester = req.RequestedBy,
                            Status = "Đã duyệt"
                        });
                    }
                }`;

content = content.replace(pendingBlockTarget, pendingBlockReplacement);
fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated NotificationService.cs');
