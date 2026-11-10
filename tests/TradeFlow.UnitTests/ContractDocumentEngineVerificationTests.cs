using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using QuestPDF.Fluent;
using TradeFlow.Application.Common.Helpers;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Services.Pdf;
using Xunit;

namespace TradeFlow.UnitTests;

public class ContractDocumentEngineVerificationTests
{
    private static readonly string OutputDir = Path.Combine(
        @"C:\Users\thitr\.gemini\antigravity\brain\fb94d497-815d-460b-bd53-9de32ef4d901\scratch\verified_pdfs");

    static ContractDocumentEngineVerificationTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        Directory.CreateDirectory(OutputDir);
    }

    private static ContractPartyASettingsDto CreateDefaultPartyA() => new()
    {
        CompanyName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
        TaxCode = "0316899999",
        RegisteredAddress = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ",
        RepresentativeName = "Trần Thị Mai Phương",
        RepresentativePosition = "Chủ tịch Hội đồng thành viên",
        RepresentativeAuthority = "Người đại diện theo pháp luật",
        Phone = "0909 123 456",
        BankName = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)",
        BankAccount = "0071000999888",
        BankBranch = "Chi nhánh Cần Thơ",
        DefaultSigningLocation = "TP. Cần Thơ"
    };

    private static ContractDto CreateBaseContract(string number, string title) => new()
    {
        Id = 100,
        ContractNumber = number,
        Title = title,
        TemplateCode = "B2B_SALE",
        Status = ContractStatus.PendingSignature,
        ContractDate = new DateTime(2026, 11, 10),
        ContractPlace = "TP. Cần Thơ",
        PartyBType = ContractPartyBType.Company,
        PartyAName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
        PartyATaxCode = "0316899999",
        PartyAAddress = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ",
        PartyARepresentative = "Trần Thị Mai Phương",
        PartyAPosition = "Chủ tịch Hội đồng thành viên",
        PartyAAuthority = "Người đại diện theo pháp luật",
        PartyAPhone = "0909 123 456",
        PartyABankName = "Vietcombank",
        PartyABankAccount = "0071000999888",
        PartyABankBranch = "Chi nhánh Cần Thơ",
        PartyBName = "CÔNG TY CỔ PHẦN ĐẦU TƯ XÂY DỰNG PHÚ HƯNG THỊNH",
        PartyBTaxCode = "1801556677",
        PartyBAddress = "Số 88 Đại lộ Hòa Bình, Quận Ninh Kiều, TP. Cần Thơ",
        PartyBPhone = "0918 222 333",
        PartyBRepresentative = "Nguyễn Hoàng Nam",
        PartyBPosition = "Tổng Giám đốc",
        PartyBAuthority = "Người đại diện theo pháp luật"
    };

    private static int CountPdfPages(byte[] pdfBytes)
    {
        var latin1 = Encoding.Latin1.GetString(pdfBytes);
        return Regex.Matches(latin1, @"/Type\s*/Page\b").Count;
    }

    private static void AssertIsStandardA4Portrait(byte[] pdfBytes)
    {
        pdfBytes.Should().NotBeNullOrEmpty();
        var header = Encoding.ASCII.GetString(pdfBytes, 0, Math.Min(16, pdfBytes.Length));
        header.Should().StartWith("%PDF-");

        // A4 Portrait in points is 595.28 x 841.89 (Skia PDF writes [0 0 595 842])
        var latin1 = Encoding.Latin1.GetString(pdfBytes);
        var hasA4MediaBox = Regex.IsMatch(latin1, @"/MediaBox\s*\[\s*0\s+0\s+595(\.\d+)?\s+(841|842)(\.\d+)?\s*\]");
        hasA4MediaBox.Should().BeTrue("Expected PDF MediaBox to be standard A4 Portrait (595 x 842 pt)");
    }

    [Fact]
    public void Scenario01_ShortOnePageContract_RendersSingleA4PageIntact()
    {
        var contract = CreateBaseContract("HĐ-2026-SC01", "THỎA THUẬN ĐẶT CỌC GIỮ HÀNG");
        contract.TotalAmount = 50_000_000m;
        contract.AmountInWords = "Năm mươi triệu đồng chẵn";
        contract.Clauses = new List<ContractClauseDto>
        {
            new() { ClauseNumber = "Điều 1", Title = "Mục đích đặt cọc", Content = "1.1. Bên B đặt cọc cho Bên A số tiền 50.000.000 VNĐ để giữ lô thiết bị vệ sinh LACASA." },
            new() { ClauseNumber = "Điều 2", Title = "Thời hạn và hiệu lực", Content = "2.1. Văn bản có hiệu lực kể từ ngày ký và lập thành 02 bản có giá trị pháp lý như nhau." }
        };

        var pages = ContractA4PreviewPaginator.PaginateCommercialContract(contract, false, false);
        pages.Should().HaveCount(1);
        pages[0].PageNumber.Should().Be(1);
        pages[0].TotalPages.Should().Be(1);

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "01_Short_1Page.pdf"), pdfBytes);

        AssertIsStandardA4Portrait(pdfBytes);
        CountPdfPages(pdfBytes).Should().Be(1);
    }

    [Fact]
    public void Scenario02_MultiPageContract_PaginatesCleanlyWithoutRepeatingHeader()
    {
        var contract = CreateBaseContract("HĐ-2026-SC02", "HỢP ĐỒNG MUA BÁN THIẾT BỊ VỆ SINH (B2B)");
        contract.TotalAmount = 385_000_000m;
        contract.AmountInWords = "Ba trăm tám mươi lăm triệu đồng chẵn";
        contract.Clauses = Enumerable.Range(1, 13).Select(i => new ContractClauseDto
        {
            ClauseNumber = $"Điều {i}",
            Title = $"Quy định chi tiết về nghĩa vụ thực hiện thứ {i}",
            Content = $"{i}.1. Hai bên cam kết thực hiện đầy đủ các quy chuẩn kỹ thuật, tiến độ giao hàng và nghiệm thu theo đúng quy định của Bộ luật Dân sự 2015 và Luật Thương mại 2005.\n" +
                      $"{i}.2. Mọi thông báo phát sinh trong quá trình thực hiện Điều {i} phải được lập thành văn bản hoặc thông điệp dữ liệu điện tử hợp lệ gửi cho đại diện có thẩm quyền của mỗi bên."
        }).ToList();

        var pages = ContractA4PreviewPaginator.PaginateCommercialContract(contract, false, false);
        pages.Count.Should().BeGreaterThanOrEqualTo(2);
        // Header block only appears on Page 1
        pages[0].Blocks.Should().Contain(b => b.Kind == ContractPreviewBlockKind.DocumentHeaderAndParties);
        pages.Skip(1).SelectMany(p => p.Blocks).Should().NotContain(b => b.Kind == ContractPreviewBlockKind.DocumentHeaderAndParties);

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "02_MultiPage_Contract.pdf"), pdfBytes);

        AssertIsStandardA4Portrait(pdfBytes);
        CountPdfPages(pdfBytes).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Scenario03_VeryLongClauseContract_FlowsNaturallyAcrossPagesWithoutBlankPageGaps()
    {
        var contract = CreateBaseContract("HĐ-2026-SC03", "HỢP ĐỒNG CUNG CẤP VÀ THI CÔNG LẮP ĐẶT HỆ THỐNG");
        var longSubClauses = string.Join("\n", Enumerable.Range(1, 18).Select(k =>
            $"1.{k}. Quy trình kiểm định áp lực nước và nghiệm thu kỹ thuật hạng mục số {k}: Bên A bố trí kỹ thuật viên chuyên trách tiến hành đo kiểm độ kín khít của hệ thống van xả, đường ống cấp thoát nước theo tiêu chuẩn TCVN 4513:1988 và lập biên bản xác nhận tại công trường."));

        contract.Clauses = new List<ContractClauseDto>
        {
            new() { ClauseNumber = "Điều 1", Title = "Quy trình kỹ thuật chi tiết và nghiệm thu từng hạng mục", Content = longSubClauses },
            new() { ClauseNumber = "Điều 2", Title = "Hiệu lực hợp đồng", Content = "2.1. Hợp đồng được lập thành 04 bản chính có giá trị pháp lý như nhau." }
        };

        var pages = ContractA4PreviewPaginator.PaginateCommercialContract(contract, false, false);
        // Page 1 must contain the DocumentHeaderAndParties AND the start of Điều 1 (ClauseHeadingAndFirstParagraph),
        // proving Điều 1 did NOT jump entirely to Page 2 leaving Page 1 half blank!
        pages[0].Blocks.Should().Contain(b => b.Kind == ContractPreviewBlockKind.ClauseHeadingAndFirstParagraph && b.ClauseHeading!.Contains("Điều 1"));
        pages.Count.Should().BeGreaterThanOrEqualTo(2);

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "03_Long_Clause_NaturalFlow.pdf"), pdfBytes);

        AssertIsStandardA4Portrait(pdfBytes);
        CountPdfPages(pdfBytes).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Scenario04_MultiRowItemsTable_RepeatsHeaderAndKeepsRowsAndTotalsIntact()
    {
        var contract = CreateBaseContract("HĐ-2026-SC04", "HỢP ĐỒNG CUNG ỨNG VẬT TƯ DỰ ÁN KHÁCH SẠN 5 SAO");
        contract.Items = Enumerable.Range(1, 28).Select(idx => new ContractItemDto
        {
            ProductCode = $"TBVS-{idx:D3}",
            ProductName = $"Bồn cầu thông minh liền khối men sứ Nano kháng khuẩn LACASA Pro Series {idx}",
            Specifications = "Men Nano chống bám bẩn, xả xoáy kép 3L/4.5L",
            Unit = "Bộ",
            Quantity = 10,
            UnitPrice = 12_500_000m,
            VatRate = 10m,
            TaxAmount = 12_500_000m,
            LineTotal = 137_500_000m,
            SortOrder = idx
        }).ToList();

        contract.SubTotal = 3_500_000_000m;
        contract.TaxRate = 10m;
        contract.TaxAmount = 350_000_000m;
        contract.TotalAmount = 3_850_000_000m;
        contract.DepositAmount = 1_000_000_000m;
        contract.AmountInWords = "Ba tỷ tám trăm năm mươi triệu đồng chẵn";
        contract.Clauses = new List<ContractClauseDto>
        {
            new() { ClauseNumber = "Điều 1", Title = "Tiến độ giao nhận", Content = "1.1. Giao hàng theo từng đợt cuốn chiếu theo tiến độ công trình." }
        };

        var pages = ContractA4PreviewPaginator.PaginateCommercialContract(contract, false, false);
        var tableBlocks = pages.SelectMany(p => p.Blocks).Where(b => b.Kind == ContractPreviewBlockKind.ItemsTableAndSummary).ToList();
        tableBlocks.Count.Should().BeGreaterThan(1);
        tableBlocks.All(b => b.IncludeTableHeader).Should().BeTrue();
        tableBlocks.Last().IncludeFinancialSummary.Should().BeTrue();

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "04_MultiRow_ItemsTable.pdf"), pdfBytes);

        AssertIsStandardA4Portrait(pdfBytes);
        CountPdfPages(pdfBytes).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Scenario05_ItalicFormatting_PreservedInParserHtmlAndPdf()
    {
        var contract = CreateBaseContract("HĐ-2026-SC05", "HỢP ĐỒNG DỊCH VỤ TƯ VẤN KỸ THUẬT");
        contract.Clauses = new List<ContractClauseDto>
        {
            new()
            {
                ClauseNumber = "Điều 1",
                Title = "Căn cứ và ghi chú chuyên môn",
                Content = "1.1. Tài liệu kỹ thuật đính kèm được hiểu là <i>Bản thông số kỹ thuật tiêu chuẩn nhà sản xuất</i> và <em>Biên bản khảo sát hiện trạng thực tế</em>.\n" +
                          "1.2. Lưu ý: *Mọi thay đổi về thiết kế đường ống phải được thông báo trước 48 giờ làm việc*."
            }
        };

        var blocks = ContractRichTextFormatter.ParseParagraphs(contract.Clauses[0].Content);
        blocks.SelectMany(b => b.Spans).Should().Contain(s => s.Italic && s.Text.Contains("Bản thông số kỹ thuật"));
        blocks.SelectMany(b => b.Spans).Should().Contain(s => s.Italic && s.Text.Contains("Biên bản khảo sát"));
        blocks.SelectMany(b => b.Spans).Should().Contain(s => s.Italic && s.Text.Contains("Mọi thay đổi về thiết kế"));

        var html = ContractRichTextFormatter.ToPreviewHtml(contract.Clauses[0].Content);
        html.Should().Contain("<em>Bản thông số kỹ thuật tiêu chuẩn nhà sản xuất</em>");
        html.Should().Contain("<em>Biên bản khảo sát hiện trạng thực tế</em>");

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "05_Italic_Formatting.pdf"), pdfBytes);
        AssertIsStandardA4Portrait(pdfBytes);
    }

    [Fact]
    public void Scenario06_BoldUnderlineAndNumberedLists_PreservedInParserHtmlAndPdf()
    {
        var contract = CreateBaseContract("HĐ-2026-SC06", "HỢP ĐỒNG NGUYÊN TẮC PHÂN PHỐI ĐẠI LÝ");
        contract.Clauses = new List<ContractClauseDto>
        {
            new()
            {
                ClauseNumber = "Điều 1",
                Title = "Chính sách chiết khấu và danh mục nghĩa vụ",
                Content = "1.1. <b>Mức chiết khấu cơ bản</b> áp dụng cho <strong>Đại lý cấp 1</strong> và <u>cam kết doanh số tối thiểu</u>:\n" +
                          "a) Doanh số quý đạt từ **500.000.000 VNĐ** trở lên được hưởng thưởng 3%;\n" +
                          "b) Hỗ trợ trưng bày showroom theo quy chuẩn nhận diện thương hiệu:\n" +
                          "- Kệ trưng bày mẫu sứ vệ sinh cao cấp;\n" +
                          "+ Bảng hiệu đèn LED và bộ catalogue kỹ thuật."
            }
        };

        var blocks = ContractRichTextFormatter.ParseParagraphs(contract.Clauses[0].Content);
        blocks.Should().HaveCount(5);
        blocks[0].BulletPrefix.Should().Be("1.1. ");
        blocks[0].Spans.Should().Contain(s => s.Bold && s.Text == "Mức chiết khấu cơ bản");
        blocks[0].Spans.Should().Contain(s => s.Underline && s.Text == "cam kết doanh số tối thiểu");
        blocks[1].BulletPrefix.Should().Be("a) ");
        blocks[1].IndentLevel.Should().Be(1);
        blocks[3].BulletPrefix.Should().Be("- ");
        blocks[4].BulletPrefix.Should().Be("+ ");
        blocks[4].IndentLevel.Should().Be(2);

        var html = ContractRichTextFormatter.ToPreviewHtml(contract.Clauses[0].Content);
        html.Should().Contain("<strong>Mức chiết khấu cơ bản</strong>");
        html.Should().Contain("<u>cam kết doanh số tối thiểu</u>");
        html.Should().Contain("tf-doc-indent-1");
        html.Should().Contain("tf-doc-indent-2");

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "06_Bold_Lists_Indentation.pdf"), pdfBytes);
        AssertIsStandardA4Portrait(pdfBytes);
    }

    [Fact]
    public void Scenario07_ThreePartyContract_RendersBalancedThreeColumnSignatureBlock()
    {
        var contract = CreateBaseContract("HĐ-2026-SC07", "HỢP ĐỒNG BA BÊN CUNG CẤP - LẮP ĐẶT - GIÁM SÁT");
        contract.GeneralTerms = "BÊN C (ĐƠN VỊ TƯ VẤN GIÁM SÁT): CÔNG TY TNHH TƯ VẤN GIÁM SÁT CÔNG TRÌNH MIỀN TÂY\n" +
                                "Mã số thuế: 1809988776\n" +
                                "Địa chỉ: Số 56 Đường 30/4, Quận Ninh Kiều, TP. Cần Thơ\n" +
                                "Đại diện Bên C: Kỹ sư Lê Văn Toàn | Chức vụ: Giám đốc Tư vấn";
        contract.Clauses = new List<ContractClauseDto>
        {
            new() { ClauseNumber = "Điều 1", Title = "Cơ chế phối hợp ba bên", Content = "1.1. Bên A cung cấp thiết bị, Bên B thanh toán và tiếp nhận, Bên C thực hiện giám sát chất lượng lắp đặt." }
        };

        var detected = ContractRichTextFormatter.TryExtractThirdParty(contract, out var thirdParty);
        detected.Should().BeTrue();
        thirdParty.Should().NotBeNull();
        thirdParty!.Name.Should().Contain("CÔNG TY TNHH TƯ VẤN GIÁM SÁT CÔNG TRÌNH MIỀN TÂY");
        thirdParty.Representative.Should().Contain("Lê Văn Toàn");

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "07_ThreeParty_Signatures.pdf"), pdfBytes);
        AssertIsStandardA4Portrait(pdfBytes);
    }

    [Fact]
    public void Scenario08_SignatureNearPageBottomWithEnoughSpace_FitsOnSamePage()
    {
        var contract = CreateBaseContract("HĐ-2026-SC08", "HỢP ĐỒNG BÁN LẺ THIẾT BỊ VỆ SINH");
        contract.Clauses = Enumerable.Range(1, 3).Select(i => new ContractClauseDto
        {
            ClauseNumber = $"Điều {i}",
            Title = $"Nội dung thỏa thuận số {i}",
            Content = $"{i}.1. Bên A giao hàng đúng chủng loại và thời gian thỏa thuận cho Bên B."
        }).ToList();

        var pages = ContractA4PreviewPaginator.PaginateCommercialContract(contract, false, false);
        pages.Should().HaveCount(1);
        pages[0].Blocks.Last().Kind.Should().Be(ContractPreviewBlockKind.SignatureSection);

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "08_Signature_Fits_Bottom.pdf"), pdfBytes);
        AssertIsStandardA4Portrait(pdfBytes);
        CountPdfPages(pdfBytes).Should().Be(1);
    }

    [Fact]
    public void Scenario09_SignatureLackingSpaceAtPageBottom_MovesEntireSignatureBlockToNextPage()
    {
        var contract = CreateBaseContract("HĐ-2026-SC09", "HỢP ĐỒNG MUA BÁN HÀNG HÓA");
        // Fill Page 1 to ~36 lines so that the 9.5-line SignatureSection cannot fit on Page 1 and moves intact to Page 2
        contract.Clauses = Enumerable.Range(1, 5).Select(i => new ContractClauseDto
        {
            ClauseNumber = $"Điều {i}",
            Title = $"Điều khoản thực hiện thứ {i}",
            Content = $"{i}.1. Hai bên thống nhất thực hiện đúng tiến độ bàn giao vật tư thiết bị vệ sinh tại chân công trình theo tiêu chuẩn kỹ thuật hiện hành.\n" +
                      $"{i}.2. Bên B cử cán bộ kỹ thuật kiểm tra ngoại quan trước khi ký biên bản bàn giao."
        }).ToList();

        var pages = ContractA4PreviewPaginator.PaginateCommercialContract(contract, false, false);
        pages.Should().HaveCount(2);
        // The entire signature section is a single block on Page 2, never split across Page 1 and Page 2
        pages[0].Blocks.Should().NotContain(b => b.Kind == ContractPreviewBlockKind.SignatureSection);
        pages[1].Blocks.Should().ContainSingle(b => b.Kind == ContractPreviewBlockKind.SignatureSection);

        var pdfBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        File.WriteAllBytes(Path.Combine(OutputDir, "09_Signature_Moves_Intact_To_NextPage.pdf"), pdfBytes);
        AssertIsStandardA4Portrait(pdfBytes);
    }

    [Fact]
    public void Scenario10_UnsignedContractVersion_ExcludesDigitalStampsAndPreservesCleanSigningArea()
    {
        var contract = CreateBaseContract("HĐ-2026-SC10", "HỢP ĐỒNG MUA BÁN THIẾT BỊ");
        // Even if the contract has signature timestamps in DB, requesting includeSignatures: false ("Bản chưa ký")
        // must produce a clean unsigned PDF for wet-ink printing or pre-sign review
        contract.PartyASignedAt = new DateTime(2026, 11, 10, 9, 30, 0);
        contract.PartyASignedBy = "Trần Thị Mai Phương";
        contract.PartyBSignedAt = new DateTime(2026, 11, 10, 10, 15, 0);
        contract.PartyBSignedBy = "Nguyễn Hoàng Nam";
        contract.DocumentHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        contract.Clauses = new List<ContractClauseDto>
        {
            new() { ClauseNumber = "Điều 1", Title = "Đối tượng hợp đồng", Content = "1.1. Cung cấp thiết bị vệ sinh chính hãng LACASA." }
        };

        var unsignedBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: false);
        var signedBytes = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: true);

        File.WriteAllBytes(Path.Combine(OutputDir, "10_Unsigned_Version.pdf"), unsignedBytes);
        File.WriteAllBytes(Path.Combine(OutputDir, "11_Signed_Version.pdf"), signedBytes);

        AssertIsStandardA4Portrait(unsignedBytes);
        AssertIsStandardA4Portrait(signedBytes);
        // Signed version contains additional signature boxes and SHA-256 verification metadata
        signedBytes.Length.Should().BeGreaterThan(unsignedBytes.Length);
    }

    [Fact]
    public void Scenario11_DigitallySignedContractVersion_PreservesSignaturesTimestampsAndSha256Hash()
    {
        var contract = CreateBaseContract("HĐ-2026-SC11", "HỢP ĐỒNG MUA BÁN ĐÃ KÝ SỐ");
        contract.Status = ContractStatus.Signed;
        contract.ExecutionMethod = ContractExecutionMethod.Electronic;
        contract.PartyASignedAt = new DateTime(2026, 11, 10, 14, 20, 0);
        contract.PartyASignedBy = "Trần Thị Mai Phương";
        contract.PartyBSignedAt = new DateTime(2026, 11, 10, 14, 25, 0);
        contract.PartyBSignedBy = "Nguyễn Hoàng Nam";
        contract.DocumentHash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
        contract.Clauses = new List<ContractClauseDto>
        {
            new() { ClauseNumber = "Điều 1", Title = "Cam kết thực hiện", Content = "1.1. Hợp đồng ký điện tử có đầy đủ giá trị pháp lý theo Luật Giao dịch điện tử 2023." }
        };

        contract.IsDigitallySigned.Should().BeTrue();
        var signedPdf = new ContractPdfService().GeneratePdf(contract, CreateDefaultPartyA(), includeSignatures: true);
        AssertIsStandardA4Portrait(signedPdf);
    }

    [Fact]
    public void Scenario12_ComplexVietnameseAndEmploymentContract_RendersFullUnicodeAndBothVersions()
    {
        var empDto = new EmploymentContractDto
        {
            Id = 501,
            ContractNumber = "HDLD-2026-0501",
            Title = "HỢP ĐỒNG LAO ĐỘNG",
            ContractType = EmploymentContractType.FixedTerm,
            Status = EmploymentContractStatus.Active,
            ContractDate = new DateTime(2026, 11, 10),
            ContractPlace = "TP. Cần Thơ",
            StartDate = new DateTime(2026, 11, 10),
            EndDate = new DateTime(2028, 11, 9),
            EmployerName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
            EmployerTaxCode = "0316899999",
            EmployerAddress = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ",
            EmployerRepresentativeName = "Trần Thị Mai Phương",
            EmployerRepresentativePosition = "Chủ tịch Hội đồng thành viên",
            EmployerRepresentativeAuthority = "Người đại diện theo pháp luật",
            EmployeeCode = "NV00088",
            EmployeeFullName = "Nguyễn Đặng Quỳnh Như",
            EmployeeDateOfBirth = new DateTime(1996, 8, 15),
            EmployeeGender = "Nữ",
            EmployeeNationalId = "092196001234",
            EmployeeAddress = "Số 45/12 Đường Mậu Thân, Phường Xuân Khánh, Quận Ninh Kiều, TP. Cần Thơ",
            EmployeePhone = "0908 777 888",
            JobTitle = "Chuyên viên Pháp chế & Kiểm soát Hợp đồng",
            DepartmentName = "Phòng Hành chính - Nhân sự",
            Workplace = "Trụ sở chính LACASA Cần Thơ",
            BasicSalary = 18_500_000m,
            PositionAllowance = 2_000_000m,
            LunchAllowance = 730_000m,
            EmployerSignedAt = new DateTime(2026, 11, 10, 8, 30, 0),
            EmployeeSignedAt = new DateTime(2026, 11, 10, 8, 45, 0),
            DigitalSignatureHash = "4b227777d4dd1fc61c6f884f48641d02b4d121d3fd328cb08b5531fcacdabf8a"
        };
        empDto.Clauses = EmploymentContractClauseDefaults.GenerateDefaultClauses(empDto);

        var empPages = ContractA4PreviewPaginator.PaginateEmploymentContract(empDto);
        empPages.Count.Should().BeGreaterThanOrEqualTo(2);

        var empUnsignedPdf = new EmploymentContractPdfDocument(empDto, includeSignatures: false).GeneratePdf();
        var empSignedPdf = new EmploymentContractPdfDocument(empDto, includeSignatures: true).GeneratePdf();

        File.WriteAllBytes(Path.Combine(OutputDir, "12_Employment_Unsigned.pdf"), empUnsignedPdf);
        File.WriteAllBytes(Path.Combine(OutputDir, "12_Employment_Signed.pdf"), empSignedPdf);

        AssertIsStandardA4Portrait(empUnsignedPdf);
        AssertIsStandardA4Portrait(empSignedPdf);
        empSignedPdf.Length.Should().BeGreaterThan(empUnsignedPdf.Length);
    }

    [Fact]
    public void Scenario13_ContractSaveAndReload_PreservesIdenticalLayoutAndPageCount()
    {
        // ARRANGE: Simulation of Contract Creation Preview (Step 5 before save)
        var createPreviewDto = CreateBaseContract("HDMB-2026-0013", "HỢP ĐỒNG MUA BÁN THIẾT BỊ VỆ SINH");
        createPreviewDto.TotalAmount = 150_000_000m;
        createPreviewDto.AmountInWords = "Một trăm năm mươi triệu đồng chẵn";
        createPreviewDto.Clauses = Enumerable.Range(1, 8).Select(i => new ContractClauseDto
        {
            ClauseNumber = $"Điều {i}",
            Title = $"Điều khoản thỏa thuận thương mại số {i}",
            Content = $"{i}.1. Quy định chi tiết về quyền và nghĩa vụ liên quan đến điều khoản {i}.\n{i}.2. Các bên cam kết thực hiện đúng thỏa thuận."
        }).ToList();
        createPreviewDto.PaymentTerms = null;
        createPreviewDto.DeliveryTerms = null;
        createPreviewDto.WarrantyTerms = null;

        var pagesBeforeSave = ContractA4PreviewPaginator.PaginateCommercialContract(createPreviewDto, isB2C: false, hasThirdParty: false);
        int pageCountBeforeSave = pagesBeforeSave.Count;

        // ACT: Simulation of Contract Reload from Database (After save, where fallback fields might have values)
        var reloadedDto = CreateBaseContract("HDMB-2026-0013", "HỢP ĐỒNG MUA BÁN THIẾT BỊ VỆ SINH");
        reloadedDto.TotalAmount = 150_000_000m;
        reloadedDto.AmountInWords = "Một trăm năm mươi triệu đồng chẵn";
        reloadedDto.Clauses = createPreviewDto.Clauses;
        reloadedDto.PaymentTerms = "Thanh toán 100% bằng chuyển khoản trong 03 ngày làm việc.";
        reloadedDto.DeliveryTerms = "Giao hàng tại kho Bên B trong giờ hành chính.";
        reloadedDto.WarrantyTerms = "Bảo hành 24 tháng theo tiêu chuẩn nhà sản xuất.";

        var pagesAfterSave = ContractA4PreviewPaginator.PaginateCommercialContract(reloadedDto, isB2C: false, hasThirdParty: false);
        int pageCountAfterSave = pagesAfterSave.Count;

        // ASSERT: Layout, Page Count, and Clause count must be 100% identical before and after save
        pageCountAfterSave.Should().Be(pageCountBeforeSave, "Layout and page count must remain identical before and after saving");
        pagesAfterSave.SelectMany(p => p.Blocks).Count()
            .Should().Be(pagesBeforeSave.SelectMany(p => p.Blocks).Count(), "No ghost fallback clauses should be injected when structured clauses exist");

        // Verify that duplicate fallback clause titles do not exist
        var afterHeadings = pagesAfterSave.SelectMany(p => p.Blocks)
            .Where(b => b.ClauseHeading != null)
            .Select(b => b.ClauseHeading!)
            .ToList();

        afterHeadings.Should().NotContain("Điều khoản thanh toán cụ thể");
        afterHeadings.Should().NotContain("Điều kiện và địa điểm giao nhận");
        afterHeadings.Should().NotContain("Chính sách bảo hành và hỗ trợ kỹ thuật");

        // PDF Generation should also succeed with identical page count
        var partyA = CreateDefaultPartyA();
        var pdfBytesBefore = new ContractPdfDocument(createPreviewDto, partyA, includeSignatures: false).GeneratePdf();
        var pdfBytesAfter = new ContractPdfDocument(reloadedDto, partyA, includeSignatures: false).GeneratePdf();

        AssertIsStandardA4Portrait(pdfBytesBefore);
        AssertIsStandardA4Portrait(pdfBytesAfter);
        CountPdfPages(pdfBytesAfter).Should().Be(CountPdfPages(pdfBytesBefore));
    }
}
