const fs = require('fs');

let c = fs.readFileSync('src/TradeFlow.Infrastructure/Services/PriceListService.cs', 'utf8');

c = c.replace('private readonly IAuditService _auditService;', 'private readonly IAuditService _auditService;\n    private readonly IFileStorageService _fileStorage;');

c = c.replace(
    'public PriceListService(\n        TradeFlowDbContext context,\n        ISystemCodeGenerator codeGenerator,\n        ICurrentUserService currentUserService,\n        IAuditService auditService)',
    'public PriceListService(\n        TradeFlowDbContext context,\n        ISystemCodeGenerator codeGenerator,\n        ICurrentUserService currentUserService,\n        IAuditService auditService,\n        IFileStorageService fileStorage)'
);

c = c.replace(
    '_auditService = auditService;\n    }',
    '_auditService = auditService;\n        _fileStorage = fileStorage;\n    }'
);

const deleteOriginalMethod = `

    public async Task<bool> DeleteOriginalFileAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PriceLists.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null || string.IsNullOrEmpty(entity.OriginalFileStorageRef))
        {
            return false;
        }

        await _fileStorage.DeleteFileAsync(entity.OriginalFileStorageRef, cancellationToken);
        entity.OriginalFileStorageRef = null;
        
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListUpdated,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            id.ToString(),
            $"Xóa file Excel gốc của bảng giá {entity.Code}",
            cancellationToken: cancellationToken);

        return true;
    }
}
`;

c = c.replace(/\}\s*$/, deleteOriginalMethod);

fs.writeFileSync('src/TradeFlow.Infrastructure/Services/PriceListService.cs', c, 'utf8');
