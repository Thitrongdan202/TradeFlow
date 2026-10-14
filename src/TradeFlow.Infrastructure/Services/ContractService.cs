using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Helpers;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Entities.Contracts;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class ContractService : IContractService
{
    private readonly TradeFlowDbContext _context;
    private readonly ISystemCodeGenerator _codeGenerator;
    private readonly IAuditService _auditService;
    private readonly IContractPdfService _pdfService;
    private readonly IFileStorageService _fileStorageService;

    public ContractService(
        TradeFlowDbContext context,
        ISystemCodeGenerator codeGenerator,
        IAuditService auditService,
        IContractPdfService pdfService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _codeGenerator = codeGenerator;
        _auditService = auditService;
        _pdfService = pdfService;
        _fileStorageService = fileStorageService;
    }

    public async Task<ContractPartyASettingsDto> GetPartyASettingsAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _context.ContractPartyASettings.FirstOrDefaultAsync(cancellationToken);
        if (entity == null)
        {
            return new ContractPartyASettingsDto
            {
                Id = 1,
                CompanyName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
                TradingName = "LACASA",
                TaxCode = "0316899999",
                RegisteredAddress = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ",
                ContactAddress = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ",
                RepresentativeName = "Nguyễn Văn An",
                RepresentativePosition = "Giám đốc",
                RepresentativeAuthority = "Người đại diện theo pháp luật",
                Phone = "0909 123 456",
                Email = "hopdong@lacasavietnam.vn",
                BankName = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)",
                BankAccount = "0071000999888",
                BankAccountHolder = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
                BankBranch = "Chi nhánh Cần Thơ",
                DefaultPaymentInstructions = "Chuyển khoản theo số tài khoản công ty hoặc tiền mặt tại quầy giao dịch",
                DefaultDeliveryTerms = "Giao hàng tại chân công trình hoặc kho bên mua trong bán kính 20km",
                DefaultWarrantyTerms = "Bảo hành chính hãng 10 năm đối với phần sứ, 03 năm đối với linh kiện và phụ kiện sen vòi"
            };
        }

        return MapPartyASettingsToDto(entity);
    }

    public async Task<ContractPartyASettingsDto> UpdatePartyASettingsAsync(ContractPartyASettingsDto dto, string updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ContractPartyASettings.FirstOrDefaultAsync(cancellationToken);
        if (entity == null)
        {
            entity = new ContractPartyASettings();
            _context.ContractPartyASettings.Add(entity);
        }

        entity.CompanyName = dto.CompanyName.Trim();
        entity.TradingName = dto.TradingName?.Trim();
        entity.TaxCode = dto.TaxCode.Trim();
        entity.BusinessRegistrationNumber = dto.BusinessRegistrationNumber?.Trim();
        entity.RegisteredAddress = dto.RegisteredAddress.Trim();
        entity.ContactAddress = dto.ContactAddress?.Trim();
        entity.RepresentativeName = dto.RepresentativeName.Trim();
        entity.RepresentativePosition = dto.RepresentativePosition.Trim();
        entity.RepresentativeAuthority = dto.RepresentativeAuthority?.Trim();
        entity.Phone = dto.Phone.Trim();
        entity.Email = dto.Email.Trim();
        entity.BankName = dto.BankName?.Trim();
        entity.BankAccount = dto.BankAccount?.Trim();
        entity.BankAccountHolder = dto.BankAccountHolder?.Trim();
        entity.BankBranch = dto.BankBranch?.Trim();
        entity.DefaultPaymentInstructions = dto.DefaultPaymentInstructions?.Trim();
        entity.DefaultDeliveryTerms = dto.DefaultDeliveryTerms?.Trim();
        entity.DefaultWarrantyTerms = dto.DefaultWarrantyTerms?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractPartyASettingsUpdated,
            updatedBy,
            "ContractPartyASettings",
            entity.Id.ToString(),
            $"Cập nhật thiết lập pháp nhân Bên A ({entity.CompanyName})");

        return MapPartyASettingsToDto(entity);
    }

    public async Task<IReadOnlyList<ContractTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _context.ContractTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken);

        return list.Select(MapTemplateToDto).ToList();
    }

    public async Task<ContractTemplateDto?> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ContractTemplates
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

        return entity == null ? null : MapTemplateToDto(entity);
    }

    public async Task<List<ContractDto>> GetContractsAsync(ContractFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Contracts
            .Include(c => c.PartnerCustomer)
            .Include(c => c.Items)
            .AsNoTracking();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c => c.ContractNumber.ToLower().Contains(term)
                    || c.Title.ToLower().Contains(term)
                    || c.PartyBName.ToLower().Contains(term)
                    || (c.PartyBTaxCode != null && c.PartyBTaxCode.ToLower().Contains(term))
                    || (c.PartyBPhone != null && c.PartyBPhone.ToLower().Contains(term)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(c => c.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.TemplateCode))
            {
                query = query.Where(c => c.TemplateCode == filter.TemplateCode);
            }

            if (filter.PartyBType.HasValue)
            {
                query = query.Where(c => c.PartyBType == filter.PartyBType.Value);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(c => c.ContractDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(c => c.ContractDate <= toUtc);
            }
        }

        var entities = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapContractToDto).ToList();
    }

    public async Task<ContractDto?> GetContractByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Contracts
            .Include(c => c.PartnerCustomer)
            .Include(c => c.Items)
            .Include(c => c.ParentContract)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity == null ? null : MapContractToDto(entity);
    }

    public async Task<ContractDto> CreateContractAsync(CreateContractDto dto, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var contractNumber = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.Contract, cancellationToken);
        var now = DateTime.UtcNow;

        var contract = new Contract
        {
            ContractNumber = contractNumber,
            Title = dto.Title.Trim(),
            TemplateCode = dto.TemplateCode,
            TemplateVersion = "1.0",
            Status = ContractStatus.Draft,
            ContractDate = NormalizeToUtc(dto.ContractDate),
            ContractPlace = string.IsNullOrWhiteSpace(dto.ContractPlace) ? "Cần Thơ" : dto.ContractPlace.Trim(),
            EffectiveDate = dto.EffectiveDate.HasValue ? NormalizeToUtc(dto.EffectiveDate.Value) : null,
            ExpirationDate = dto.ExpirationDate.HasValue ? NormalizeToUtc(dto.ExpirationDate.Value) : null,
            PartyBType = dto.PartyBType,
            PartnerId = dto.PartnerId,

            // Party A Snapshot
            PartyAName = dto.PartyAName.Trim(),
            PartyATaxCode = dto.PartyATaxCode?.Trim(),
            PartyAAddress = dto.PartyAAddress?.Trim(),
            PartyAContactAddress = dto.PartyAContactAddress?.Trim(),
            PartyARepresentative = dto.PartyARepresentative?.Trim(),
            PartyAPosition = dto.PartyAPosition?.Trim(),
            PartyAAuthority = dto.PartyAAuthority?.Trim(),
            PartyAPhone = dto.PartyAPhone?.Trim(),
            PartyAEmail = dto.PartyAEmail?.Trim(),
            PartyABankName = dto.PartyABankName?.Trim(),
            PartyABankAccount = dto.PartyABankAccount?.Trim(),
            PartyABankAccountHolder = dto.PartyABankAccountHolder?.Trim(),
            PartyABankBranch = dto.PartyABankBranch?.Trim(),

            // Party B Snapshot
            PartyBName = dto.PartyBName.Trim(),
            PartyBTaxCode = dto.PartyBTaxCode?.Trim(),
            PartyBIdCardNumber = dto.PartyBIdCardNumber?.Trim(),
            PartyBIdCardDate = dto.PartyBIdCardDate?.Trim(),
            PartyBIdCardPlace = dto.PartyBIdCardPlace?.Trim(),
            PartyBAddress = dto.PartyBAddress?.Trim(),
            PartyBDeliveryAddress = dto.PartyBDeliveryAddress?.Trim(),
            PartyBPhone = dto.PartyBPhone?.Trim(),
            PartyBEmail = dto.PartyBEmail?.Trim(),
            PartyBRepresentative = dto.PartyBRepresentative?.Trim(),
            PartyBPosition = dto.PartyBPosition?.Trim(),
            PartyBAuthority = dto.PartyBAuthority?.Trim(),
            PartyBBankName = dto.PartyBBankName?.Trim(),
            PartyBBankAccount = dto.PartyBBankAccount?.Trim(),

            // Terms
            PaymentTerms = dto.PaymentTerms?.Trim(),
            DeliveryTerms = dto.DeliveryTerms?.Trim(),
            InstallationTerms = dto.InstallationTerms?.Trim(),
            WarrantyTerms = dto.WarrantyTerms?.Trim(),
            BreachPenaltyTerms = dto.BreachPenaltyTerms?.Trim(),
            DisputeResolutionTerms = dto.DisputeResolutionTerms?.Trim(),
            GeneralTerms = dto.GeneralTerms?.Trim(),
            CustomClausesJson = JsonSerializer.Serialize(dto.Clauses ?? new List<ContractClauseDto>()),

            ParentContractId = dto.ParentContractId,
            Notes = dto.Notes?.Trim(),
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            CreatedAt = now,
            CreatedBy = userName
        };

        // Line Items & Financials
        int sortOrder = 1;
        decimal subTotal = 0;
        decimal totalTax = 0;
        decimal totalDiscount = 0;

        foreach (var itemDto in dto.Items)
        {
            var lineTotal = (itemDto.Quantity * itemDto.UnitPrice) - itemDto.DiscountAmount;
            var taxAmount = Math.Round(lineTotal * (itemDto.VatRate / 100m), 0);
            var itemTotal = lineTotal + taxAmount;

            subTotal += lineTotal;
            totalTax += taxAmount;
            totalDiscount += itemDto.DiscountAmount;

            contract.Items.Add(new ContractItem
            {
                ProductId = itemDto.ProductId,
                ProductCode = itemDto.ProductCode?.Trim() ?? string.Empty,
                ProductName = itemDto.ProductName?.Trim() ?? string.Empty,
                Unit = itemDto.Unit?.Trim() ?? "Bộ",
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                DiscountAmount = itemDto.DiscountAmount,
                VatRate = itemDto.VatRate,
                TaxAmount = taxAmount,
                LineTotal = itemTotal,
                Specifications = itemDto.Specifications?.Trim(),
                SortOrder = sortOrder++,
                Note = itemDto.Note?.Trim(),
                CreatedAt = now,
                CreatedBy = userName
            });
        }

        contract.SubTotal = subTotal;
        contract.TotalDiscount = totalDiscount;
        contract.TaxRate = dto.TaxRate;
        contract.TaxAmount = totalTax;
        contract.TotalAmount = subTotal + totalTax;
        contract.DepositAmount = dto.DepositAmount;
        contract.RemainingAmount = Math.Max(0, contract.TotalAmount - contract.DepositAmount);
        contract.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? "VND" : dto.CurrencyCode;
        contract.AmountInWords = VietnameseNumberToWords.ConvertToWords(contract.TotalAmount);
        contract.DocumentHash = ComputeDocumentHash(contract);

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractCreated,
            userName,
            "Contract",
            contract.Id.ToString(),
            $"Tạo hợp đồng mới {contract.ContractNumber}: {contract.Title} (Bên B: {contract.PartyBName}, Giá trị: {contract.TotalAmount:N0} đ)");

        // Sync to DocumentAttachment for "Chứng từ" integration
        await SyncContractDocumentAttachmentAsync(contract, cancellationToken);

        return MapContractToDto(contract);
    }

    public async Task<ContractDto> UpdateContractAsync(int id, UpdateContractDto dto, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        if (contract.Status == ContractStatus.Signed || contract.Status == ContractStatus.Liquidated || contract.Status == ContractStatus.Cancelled)
            throw new InvalidOperationException("Không thể chỉnh sửa hợp đồng đã được ký kết, thanh lý hoặc hủy bỏ.");

        var now = DateTime.UtcNow;
        contract.Title = dto.Title.Trim();
        contract.ContractDate = NormalizeToUtc(dto.ContractDate);
        contract.ContractPlace = string.IsNullOrWhiteSpace(dto.ContractPlace) ? "Cần Thơ" : dto.ContractPlace.Trim();
        contract.EffectiveDate = dto.EffectiveDate.HasValue ? NormalizeToUtc(dto.EffectiveDate.Value) : null;
        contract.ExpirationDate = dto.ExpirationDate.HasValue ? NormalizeToUtc(dto.ExpirationDate.Value) : null;
        contract.PartyBType = dto.PartyBType;
        contract.PartnerId = dto.PartnerId;

        // Party A Snapshot
        contract.PartyAName = dto.PartyAName.Trim();
        contract.PartyATaxCode = dto.PartyATaxCode?.Trim();
        contract.PartyAAddress = dto.PartyAAddress?.Trim();
        contract.PartyAContactAddress = dto.PartyAContactAddress?.Trim();
        contract.PartyARepresentative = dto.PartyARepresentative?.Trim();
        contract.PartyAPosition = dto.PartyAPosition?.Trim();
        contract.PartyAAuthority = dto.PartyAAuthority?.Trim();
        contract.PartyAPhone = dto.PartyAPhone?.Trim();
        contract.PartyAEmail = dto.PartyAEmail?.Trim();
        contract.PartyABankName = dto.PartyABankName?.Trim();
        contract.PartyABankAccount = dto.PartyABankAccount?.Trim();
        contract.PartyABankAccountHolder = dto.PartyABankAccountHolder?.Trim();
        contract.PartyABankBranch = dto.PartyABankBranch?.Trim();

        // Party B Snapshot
        contract.PartyBName = dto.PartyBName.Trim();
        contract.PartyBTaxCode = dto.PartyBTaxCode?.Trim();
        contract.PartyBIdCardNumber = dto.PartyBIdCardNumber?.Trim();
        contract.PartyBIdCardDate = dto.PartyBIdCardDate?.Trim();
        contract.PartyBIdCardPlace = dto.PartyBIdCardPlace?.Trim();
        contract.PartyBAddress = dto.PartyBAddress?.Trim();
        contract.PartyBDeliveryAddress = dto.PartyBDeliveryAddress?.Trim();
        contract.PartyBPhone = dto.PartyBPhone?.Trim();
        contract.PartyBEmail = dto.PartyBEmail?.Trim();
        contract.PartyBRepresentative = dto.PartyBRepresentative?.Trim();
        contract.PartyBPosition = dto.PartyBPosition?.Trim();
        contract.PartyBAuthority = dto.PartyBAuthority?.Trim();
        contract.PartyBBankName = dto.PartyBBankName?.Trim();
        contract.PartyBBankAccount = dto.PartyBBankAccount?.Trim();

        // Terms
        contract.PaymentTerms = dto.PaymentTerms?.Trim();
        contract.DeliveryTerms = dto.DeliveryTerms?.Trim();
        contract.InstallationTerms = dto.InstallationTerms?.Trim();
        contract.WarrantyTerms = dto.WarrantyTerms?.Trim();
        contract.BreachPenaltyTerms = dto.BreachPenaltyTerms?.Trim();
        contract.DisputeResolutionTerms = dto.DisputeResolutionTerms?.Trim();
        contract.GeneralTerms = dto.GeneralTerms?.Trim();
        contract.CustomClausesJson = JsonSerializer.Serialize(dto.Clauses ?? new List<ContractClauseDto>());
        contract.Notes = dto.Notes?.Trim();
        contract.UpdatedAt = now;
        contract.UpdatedBy = userName;

        // Re-process Items
        _context.ContractItems.RemoveRange(contract.Items);
        contract.Items.Clear();

        int sortOrder = 1;
        decimal subTotal = 0;
        decimal totalTax = 0;
        decimal totalDiscount = 0;

        foreach (var itemDto in dto.Items)
        {
            var lineTotal = (itemDto.Quantity * itemDto.UnitPrice) - itemDto.DiscountAmount;
            var taxAmount = Math.Round(lineTotal * (itemDto.VatRate / 100m), 0);
            var itemTotal = lineTotal + taxAmount;

            subTotal += lineTotal;
            totalTax += taxAmount;
            totalDiscount += itemDto.DiscountAmount;

            contract.Items.Add(new ContractItem
            {
                ProductId = itemDto.ProductId,
                ProductCode = itemDto.ProductCode?.Trim() ?? string.Empty,
                ProductName = itemDto.ProductName?.Trim() ?? string.Empty,
                Unit = itemDto.Unit?.Trim() ?? "Bộ",
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                DiscountAmount = itemDto.DiscountAmount,
                VatRate = itemDto.VatRate,
                TaxAmount = taxAmount,
                LineTotal = itemTotal,
                Specifications = itemDto.Specifications?.Trim(),
                SortOrder = sortOrder++,
                Note = itemDto.Note?.Trim(),
                CreatedAt = now,
                CreatedBy = userName
            });
        }

        contract.SubTotal = subTotal;
        contract.TotalDiscount = totalDiscount;
        contract.TaxRate = dto.TaxRate;
        contract.TaxAmount = totalTax;
        contract.TotalAmount = subTotal + totalTax;
        contract.DepositAmount = dto.DepositAmount;
        contract.RemainingAmount = Math.Max(0, contract.TotalAmount - contract.DepositAmount);
        contract.AmountInWords = VietnameseNumberToWords.ConvertToWords(contract.TotalAmount);
        contract.DocumentHash = ComputeDocumentHash(contract);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractUpdated,
            userName,
            "Contract",
            contract.Id.ToString(),
            $"Cập nhật hợp đồng {contract.ContractNumber}: {contract.Title}");

        await SyncContractDocumentAttachmentAsync(contract, cancellationToken);

        return MapContractToDto(contract);
    }

    public async Task<bool> DeleteContractAsync(int id, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null) return false;

        if (contract.Status != ContractStatus.Draft)
            throw new InvalidOperationException("Chỉ có thể xóa hợp đồng khi đang ở trạng thái Bản nháp (Draft).");

        _context.ContractItems.RemoveRange(contract.Items);
        _context.Contracts.Remove(contract);

        // Remove document attachment if any
        var doc = await _context.DocumentAttachments
            .FirstOrDefaultAsync(d => d.RelatedEntityType == "Contract" && d.RelatedEntityId == id, cancellationToken);
        if (doc != null)
        {
            _context.DocumentAttachments.Remove(doc);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractDeleted,
            userName,
            "Contract",
            contract.Id.ToString(),
            $"Xóa hợp đồng {contract.ContractNumber} ({contract.Title})");

        return true;
    }

    public async Task<ContractDto> FinalizeContractAsync(int id, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        contract.Status = ContractStatus.PendingSignature;
        contract.FinalizedAt = DateTime.UtcNow;
        contract.FinalizedBy = userName;
        contract.DocumentHash = ComputeDocumentHash(contract);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractFinalized,
            userName,
            "Contract",
            contract.Id.ToString(),
            $"Chốt nội dung hợp đồng {contract.ContractNumber}, chuyển sang trạng thái Chờ ký");

        await SyncContractDocumentAttachmentAsync(contract, cancellationToken);

        return MapContractToDto(contract);
    }

    public async Task<ContractDto> SignContractAsync(int id, SignContractDto dto, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        var now = DateTime.UtcNow;

        if (string.Equals(dto.SignerParty, "PartyA", StringComparison.OrdinalIgnoreCase))
        {
            contract.PartyASignedAt = now;
            contract.PartyASignedBy = dto.SignerName.Trim();
            contract.PartyASignatureData = dto.SignatureData;
        }
        else
        {
            contract.PartyBSignedAt = now;
            contract.PartyBSignedBy = dto.SignerName.Trim();
            contract.PartyBSignatureData = dto.SignatureData;
        }

        if (contract.PartyASignedAt.HasValue && contract.PartyBSignedAt.HasValue)
        {
            contract.Status = ContractStatus.Signed;
            contract.ExecutionMethod = contract.ExecutionMethod == ContractExecutionMethod.PaperPrinted 
                ? ContractExecutionMethod.Both 
                : ContractExecutionMethod.Electronic;
        }
        else
        {
            contract.Status = ContractStatus.PendingSignature;
        }

        contract.DocumentHash = ComputeDocumentHash(contract);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractSigned,
            dto.SignerName,
            "Contract",
            contract.Id.ToString(),
            $"Ký điện tử hợp đồng {contract.ContractNumber} ({dto.SignerParty})");

        await SyncContractDocumentAttachmentAsync(contract, cancellationToken);

        return MapContractToDto(contract);
    }

    public async Task<ContractDto> RecordPrintedAsync(int id, string printedBy, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        contract.IsPaperPrinted = true;
        contract.PrintedAt = DateTime.UtcNow;
        contract.PrintedBy = printedBy;

        if (contract.Status == ContractStatus.Draft || contract.Status == ContractStatus.PendingSignature)
        {
            contract.Status = ContractStatus.Printed;
            contract.ExecutionMethod = ContractExecutionMethod.PaperPrinted;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractPrinted,
            printedBy,
            "Contract",
            contract.Id.ToString(),
            $"Ghi nhận in hợp đồng giấy {contract.ContractNumber}");

        return MapContractToDto(contract);
    }

    public async Task<ContractDto> RecordPaperSignedAsync(int id, int attachmentId, string signedBy, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        contract.PaperSignedScanAttachmentId = attachmentId;
        contract.Status = ContractStatus.Signed;
        contract.ExecutionMethod = contract.ExecutionMethod == ContractExecutionMethod.Electronic 
            ? ContractExecutionMethod.Both 
            : ContractExecutionMethod.PaperPrinted;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractSigned,
            signedBy,
            "Contract",
            contract.Id.ToString(),
            $"Ghi nhận bản quét hợp đồng đã ký giấy {contract.ContractNumber} (Đính kèm #{attachmentId})");

        return MapContractToDto(contract);
    }

    public async Task<ContractDto> LiquidateContractAsync(int id, string reason, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        contract.Status = ContractStatus.Liquidated;
        contract.Notes = (contract.Notes + $" | Thanh lý: {reason}").TrimStart(' ', '|');
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = userName;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractLiquidated,
            userName,
            "Contract",
            contract.Id.ToString(),
            $"Thanh lý hợp đồng {contract.ContractNumber}. Lý do: {reason}");

        return MapContractToDto(contract);
    }

    public async Task<ContractDto> CancelContractAsync(int id, string reason, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        contract.Status = ContractStatus.Cancelled;
        contract.Notes = (contract.Notes + $" | Hủy bỏ: {reason}").TrimStart(' ', '|');
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = userName;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ContractCancelled,
            userName,
            "Contract",
            contract.Id.ToString(),
            $"Hủy hợp đồng {contract.ContractNumber}. Lý do: {reason}");

        return MapContractToDto(contract);
    }

    public async Task<byte[]> GenerateContractPdfAsync(int id, CancellationToken cancellationToken = default)
    {
        var contract = await GetContractByIdAsync(id, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng ID {id}");

        var partyASettings = await GetPartyASettingsAsync(cancellationToken);
        return _pdfService.GeneratePdf(contract, partyASettings);
    }

    // ==========================================
    // PRIVATE HELPERS
    // ==========================================

    private async Task SyncContractDocumentAttachmentAsync(Contract contract, CancellationToken cancellationToken)
    {
        try
        {
            var cat = await _context.DocumentCategories
                .FirstOrDefaultAsync(c => c.Code == "HOP_DONG", cancellationToken);

            var existing = await _context.DocumentAttachments
                .FirstOrDefaultAsync(d => d.RelatedEntityType == "Contract" && d.RelatedEntityId == contract.Id, cancellationToken);

            var fileName = $"{contract.ContractNumber}_{SanitizeFileName(contract.Title)}.pdf";

            if (existing == null)
            {
                existing = new DocumentAttachment
                {
                    FileName = fileName,
                    StoragePath = $"/documents/contracts/{fileName}",
                    ContentType = "application/pdf",
                    FileSize = 0,
                    CategoryId = cat?.Id,
                    RelatedEntityType = "Contract",
                    RelatedEntityId = contract.Id,
                    Description = $"Hợp đồng {contract.ContractNumber} - {contract.Title} (Bên B: {contract.PartyBName})",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = contract.CreatedByUserName
                };
                _context.DocumentAttachments.Add(existing);
            }
            else
            {
                existing.FileName = fileName;
                existing.Description = $"Hợp đồng {contract.ContractNumber} - {contract.Title} (Bên B: {contract.PartyBName})";
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = contract.CreatedByUserName;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Non-critical background sync
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Where(c => !invalid.Contains(c) && c != ' ' && c != '/').ToArray());
        return clean.Length > 30 ? clean[..30] : clean;
    }

    private static DateTime NormalizeToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified || date.Kind == DateTimeKind.Local)
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return date;
    }

    private static string ComputeDocumentHash(Contract c)
    {
        var raw = $"{c.ContractNumber}|{c.TemplateCode}|{c.Title}|{c.ContractDate:O}|{c.PartyAName}|{c.PartyATaxCode}|{c.PartyBName}|{c.PartyBTaxCode ?? c.PartyBIdCardNumber}|{c.TotalAmount}|{c.CustomClausesJson}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static ContractPartyASettingsDto MapPartyASettingsToDto(ContractPartyASettings e)
    {
        return new ContractPartyASettingsDto
        {
            Id = e.Id,
            CompanyName = e.CompanyName,
            TradingName = e.TradingName,
            TaxCode = e.TaxCode,
            BusinessRegistrationNumber = e.BusinessRegistrationNumber,
            RegisteredAddress = e.RegisteredAddress,
            ContactAddress = e.ContactAddress,
            RepresentativeName = e.RepresentativeName,
            RepresentativePosition = e.RepresentativePosition,
            RepresentativeAuthority = e.RepresentativeAuthority,
            Phone = e.Phone,
            Email = e.Email,
            BankName = e.BankName,
            BankAccount = e.BankAccount,
            BankAccountHolder = e.BankAccountHolder,
            BankBranch = e.BankBranch,
            DefaultPaymentInstructions = e.DefaultPaymentInstructions,
            DefaultDeliveryTerms = e.DefaultDeliveryTerms,
            DefaultWarrantyTerms = e.DefaultWarrantyTerms
        };
    }

    private static ContractTemplateDto MapTemplateToDto(ContractTemplate t)
    {
        var clauses = new List<ContractClauseDto>();
        try
        {
            if (!string.IsNullOrWhiteSpace(t.DefaultClausesJson))
            {
                clauses = JsonSerializer.Deserialize<List<ContractClauseDto>>(t.DefaultClausesJson) ?? new();
            }
        }
        catch { }

        return new ContractTemplateDto
        {
            Id = t.Id,
            Code = t.Code,
            Title = t.Title,
            Category = t.Category,
            Description = t.Description,
            TypicalUseCase = t.TypicalUseCase,
            TargetPartyType = t.TargetPartyType,
            RequiredInfo = t.RequiredInfo,
            Icon = t.Icon,
            Version = t.Version,
            IsActive = t.IsActive,
            DisplayOrder = t.DisplayOrder,
            LegalBasis = t.LegalBasis,
            KnownLimitations = t.KnownLimitations,
            IsReviewedByLegal = t.IsReviewedByLegal,
            DefaultClausesJson = t.DefaultClausesJson,
            Clauses = clauses
        };
    }

    private static ContractDto MapContractToDto(Contract c)
    {
        var clauses = new List<ContractClauseDto>();
        try
        {
            if (!string.IsNullOrWhiteSpace(c.CustomClausesJson))
            {
                clauses = JsonSerializer.Deserialize<List<ContractClauseDto>>(c.CustomClausesJson) ?? new();
            }
        }
        catch { }

        return new ContractDto
        {
            Id = c.Id,
            ContractNumber = c.ContractNumber,
            Title = c.Title,
            TemplateCode = c.TemplateCode,
            TemplateVersion = c.TemplateVersion,
            Status = c.Status,
            ContractDate = c.ContractDate,
            ContractPlace = c.ContractPlace,
            EffectiveDate = c.EffectiveDate,
            ExpirationDate = c.ExpirationDate,
            PartyBType = c.PartyBType,
            PartnerId = c.PartnerId,
            PartnerCustomerName = c.PartnerCustomer?.Name ?? c.PartyBName,

            // Party A
            PartyAName = c.PartyAName,
            PartyATaxCode = c.PartyATaxCode,
            PartyAAddress = c.PartyAAddress,
            PartyAContactAddress = c.PartyAContactAddress,
            PartyARepresentative = c.PartyARepresentative,
            PartyAPosition = c.PartyAPosition,
            PartyAAuthority = c.PartyAAuthority,
            PartyAPhone = c.PartyAPhone,
            PartyAEmail = c.PartyAEmail,
            PartyABankName = c.PartyABankName,
            PartyABankAccount = c.PartyABankAccount,
            PartyABankAccountHolder = c.PartyABankAccountHolder,
            PartyABankBranch = c.PartyABankBranch,

            // Party B
            PartyBName = c.PartyBName,
            PartyBTaxCode = c.PartyBTaxCode,
            PartyBIdCardNumber = c.PartyBIdCardNumber,
            PartyBIdCardDate = c.PartyBIdCardDate,
            PartyBIdCardPlace = c.PartyBIdCardPlace,
            PartyBAddress = c.PartyBAddress,
            PartyBDeliveryAddress = c.PartyBDeliveryAddress,
            PartyBPhone = c.PartyBPhone,
            PartyBEmail = c.PartyBEmail,
            PartyBRepresentative = c.PartyBRepresentative,
            PartyBPosition = c.PartyBPosition,
            PartyBAuthority = c.PartyBAuthority,
            PartyBBankName = c.PartyBBankName,
            PartyBBankAccount = c.PartyBBankAccount,

            // Financial
            SubTotal = c.SubTotal,
            TotalDiscount = c.TotalDiscount,
            TaxRate = c.TaxRate,
            TaxAmount = c.TaxAmount,
            TotalAmount = c.TotalAmount,
            DepositAmount = c.DepositAmount,
            RemainingAmount = c.RemainingAmount,
            CurrencyCode = c.CurrencyCode,
            AmountInWords = c.AmountInWords,

            // Terms
            PaymentTerms = c.PaymentTerms,
            DeliveryTerms = c.DeliveryTerms,
            InstallationTerms = c.InstallationTerms,
            WarrantyTerms = c.WarrantyTerms,
            BreachPenaltyTerms = c.BreachPenaltyTerms,
            DisputeResolutionTerms = c.DisputeResolutionTerms,
            GeneralTerms = c.GeneralTerms,
            CustomClausesJson = c.CustomClausesJson,
            Clauses = clauses,

            // Execution
            ExecutionMethod = c.ExecutionMethod,
            PartyASignedAt = c.PartyASignedAt,
            PartyASignedBy = c.PartyASignedBy,
            PartyASignatureData = c.PartyASignatureData,
            PartyBSignedAt = c.PartyBSignedAt,
            PartyBSignedBy = c.PartyBSignedBy,
            PartyBSignatureData = c.PartyBSignatureData,
            DocumentHash = c.DocumentHash,

            IsPaperPrinted = c.IsPaperPrinted,
            PrintedAt = c.PrintedAt,
            PrintedBy = c.PrintedBy,
            PaperSignedScanAttachmentId = c.PaperSignedScanAttachmentId,

            CreatedByUserId = c.CreatedByUserId,
            CreatedByUserName = c.CreatedByUserName,
            FinalizedAt = c.FinalizedAt,
            FinalizedBy = c.FinalizedBy,

            ParentContractId = c.ParentContractId,
            ParentContractNumber = c.ParentContract?.ContractNumber,
            Notes = c.Notes,
            CreatedAt = c.CreatedAt,
            CreatedBy = c.CreatedBy,

            Items = c.Items.OrderBy(i => i.SortOrder).Select(i => new ContractItemDto
            {
                Id = i.Id,
                ContractId = i.ContractId,
                ProductId = i.ProductId,
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                Unit = i.Unit,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                VatRate = i.VatRate,
                TaxAmount = i.TaxAmount,
                LineTotal = i.LineTotal,
                Specifications = i.Specifications,
                SortOrder = i.SortOrder,
                Note = i.Note
            }).ToList()
        };
    }
}
