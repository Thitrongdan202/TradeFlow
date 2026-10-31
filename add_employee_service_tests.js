const fs = require('fs');

const path = 'tests/TradeFlow.UnitTests/Employees/EmployeeServiceTests.cs';
let content = fs.readFileSync(path, 'utf8');

const additionalTests = `
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
`;

const lastBraceIdx = content.lastIndexOf('}');
content = content.substring(0, lastBraceIdx) + additionalTests + '\n}\n';
fs.writeFileSync(path, content, 'utf8');
console.log('Added 3 new workflow unit tests to EmployeeServiceTests.cs');
