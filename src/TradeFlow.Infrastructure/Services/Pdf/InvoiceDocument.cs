using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class InvoiceDocument : IDocument
{
    private readonly InvoiceDto _model;

    public InvoiceDocument(InvoiceDto model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    void ComposeHeader(IContainer container)
    {
        bool isVat = _model.Type == InvoiceType.VatInvoice;
        string title = isVat ? "HÓA ĐƠN GIÁ TRỊ GIA TĂNG" : "HÓA ĐƠN BÁN HÀNG";
        string titleEng = isVat ? "(VAT INVOICE)" : "(SALES INVOICE)";
        string formNo = isVat ? "01GTKT0/001" : "02GTTT0/001";
        
        container.Row(row =>
        {
            // Logo column
            row.ConstantItem(120).AlignCenter().AlignMiddle().Column(c =>
            {
                if (!string.IsNullOrEmpty(_model.CompanyLogoUrl) && File.Exists(_model.CompanyLogoUrl))
                {
                    c.Item().Width(80).Image(_model.CompanyLogoUrl);
                }
            });

            // Title column
            row.RelativeItem().AlignCenter().Column(c =>
            {
                c.Item().Text(title).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                c.Item().Text(titleEng).FontSize(11).FontColor(Colors.Blue.Darken2);
                c.Item().PaddingTop(5).Text($"Ngày (day) {_model.InvoiceDate:dd} tháng (month) {_model.InvoiceDate:MM} năm (year) {_model.InvoiceDate:yyyy}").FontSize(10).Italic();
            });

            // Meta column
            row.ConstantItem(150).Column(c =>
            {
                c.Item().Row(r => { r.RelativeItem().Text("Mẫu số (Form No.):"); r.RelativeItem().Text(formNo); });
                c.Item().Row(r => { r.RelativeItem().Text("Ký hiệu (Serial No.):"); r.RelativeItem().Text("1C26TFL"); });
                c.Item().Row(r => { r.RelativeItem().Text("Số (Invoice No.):"); r.RelativeItem().Text(_model.InvoiceNumber).Bold(); });
            });
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);

            // Seller Info
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(120);
                    c.RelativeColumn();
                });
                
                table.Cell().Text("Đơn vị bán (Seller):");
                table.Cell().Text(_model.CompanyName).Bold();
                
                table.Cell().Text("MST (Tax Code):");
                table.Cell().Text(_model.CompanyTaxCode ?? "");

                table.Cell().Text("Địa chỉ (Address):");
                table.Cell().Text(_model.CompanyAddress ?? "");

                table.Cell().Text("Điện thoại (Tel.):");
                table.Cell().Text(_model.CompanyPhone ?? "");

                table.Cell().Text("STK (Account No.):");
                table.Cell().Text(_model.CompanyBankAccount ?? "");
            });

            column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Buyer Info
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(120);
                    c.RelativeColumn();
                });
                
                table.Cell().Text("Người mua (Buyer):");
                table.Cell().Text(_model.CustomerName).Bold();
                
                table.Cell().Text("Đơn vị (Co. name):");
                table.Cell().Text(_model.CustomerCompanyName ?? "");
                
                table.Cell().Text("MST/CCCD (Tax Code):");
                table.Cell().Text(_model.CustomerTaxCode ?? "");

                table.Cell().Text("Địa chỉ (Address):");
                table.Cell().Text(_model.CustomerAddress ?? "");

                table.Cell().Row(r =>
                {
                    r.RelativeItem().Row(rr =>
                    {
                        rr.ConstantItem(120).Text("HTTT (Pay. method):");
                        rr.RelativeItem().Text(_model.PaymentMethod ?? "TM/CK");
                    });
                });
                
                table.Cell().Text("STK (Account No.):");
                table.Cell().Text(_model.CustomerBankAccount ?? "");
            });

            column.Item().PaddingTop(15).Element(ComposeTable);

            // Totals
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(250).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    if (_model.Type == InvoiceType.VatInvoice)
                    {
                        table.Cell().AlignRight().PaddingRight(10).Text("Cộng tiền hàng (Sub total):");
                        table.Cell().AlignRight().Text(_model.SubTotal.ToString("N0"));

                        table.Cell().AlignRight().PaddingRight(10).Text("Cộng tiền thuế GTGT (VAT amount):");
                        table.Cell().AlignRight().Text(_model.TotalTax.ToString("N0"));
                    }

                    table.Cell().AlignRight().PaddingRight(10).Text("Tổng cộng tiền thanh toán (Total):").Bold();
                    table.Cell().AlignRight().Text(_model.GrandTotal.ToString("N0")).Bold();
                });
            });

            // Amount in words
            column.Item().PaddingTop(10).Row(row =>
            {
                row.ConstantItem(220).Text("Số tiền viết bằng chữ (Amount in words):").Italic();
                row.RelativeItem().Text(TradeFlow.Application.Common.Helpers.NumberToTextHelper.ConvertToWords((long)_model.GrandTotal) + " đồng chẵn.").Bold().Italic();
            });

            // Signatures
            column.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("Người mua hàng (Buyer)").Bold();
                    c.Item().Text("(Ký, ghi rõ họ tên)").FontSize(9).Italic();
                });

                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("Người bán hàng (Seller)").Bold();
                    c.Item().Text("(Ký, ghi rõ họ tên)").FontSize(9).Italic();
                    
                    if (!string.IsNullOrEmpty(_model.CompanyName))
                    {
                        c.Item().PaddingTop(20).Container().Border(1).BorderColor(Colors.Green.Darken1).Background(Colors.Green.Lighten5).Padding(10).Column(sig => 
                        {
                            sig.Item().Text("Signature Valid").Bold().FontColor(Colors.Green.Darken2);
                            sig.Item().Text($"Ký bởi: {_model.CompanyName}").Bold().FontColor(Colors.Red.Darken2);
                            sig.Item().Text($"Ký ngày: {DateTime.Now:dd/MM/yyyy}").Bold().FontColor(Colors.Red.Darken2);
                        });
                    }
                });
            });
        });
    }

    void ComposeTable(IContainer container)
    {
        bool isVat = _model.Type == InvoiceType.VatInvoice;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30); // STT
                columns.RelativeColumn(3); // Name
                columns.ConstantColumn(50); // Unit
                columns.ConstantColumn(40); // Qty
                columns.RelativeColumn(); // Unit Price
                columns.RelativeColumn(); // Amount

                if (isVat)
                {
                    columns.ConstantColumn(40); // Tax Rate
                    columns.RelativeColumn(); // Tax Amount
                }
            });

            table.Header(header =>
            {
                header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("STT").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("Tên hàng hoá, dịch vụ").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("ĐVT").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("SL").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("Đơn giá").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("Thành tiền").Bold();
                
                if (isVat)
                {
                    header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("Thuế %").Bold();
                    header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Blue.Lighten4).Padding(4).AlignCenter().Text("Tiền thuế").Bold();
                }
            });

            int stt = 1;
            foreach (var item in _model.Items)
            {
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignCenter().Text(stt.ToString());
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(item.ProductName);
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignCenter().Text(item.UnitName);
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text(item.Quantity.ToString("G29"));
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text(item.UnitPrice.ToString("N0"));
                
                var totalBeforeTax = item.Quantity * item.UnitPrice - item.DiscountAmount;
                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text(totalBeforeTax.ToString("N0"));

                if (isVat)
                {
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text($"{item.TaxRate}%");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text(item.TaxAmount.ToString("N0"));
                }
                stt++;
            }
        });
    }

    void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text("(Cần kiểm tra đối chiếu khi lập, giao, nhận hóa đơn)").FontSize(10).Italic();
    }
}
