using System;
using System.Collections.Generic;

namespace TradeFlow.Application.Common.Models.Sales;

public class OrderDocumentDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty; // VD: DH000578
    public DateTime OrderDate { get; set; } = DateTime.Now;

    // Thông tin khách hàng
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }

    // Thông tin đơn vị bán
    public string CompanyName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Hotline { get; set; }

    // Danh sách sản phẩm
    public List<OrderDocumentItemDto> Items { get; set; } = new();

    // Tổng cộng
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal GrandTotal { get; set; }

    // Ghi chú & Ngân hàng
    public string? CustomerNotes { get; set; }
    public string? VatNote { get; set; }
    public string? BankAccountHolder { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? QrCodePath { get; set; }
    public string? FooterNote1 { get; set; }
    public string? FooterNote2 { get; set; }
}

public class OrderDocumentItemDto
{
    public int No { get; set; }
    public string CategoryName { get; set; } = string.Empty; // Cột "Sản Phẩm" (VD: LAVABO ĐỂ BÀN)
    public string ProductCode { get; set; } = string.Empty;  // Cột "Tên hàng" (VD: LVB205-LS)
    public string Description { get; set; } = string.Empty;  // Cột "Mô Tả Sản Phẩm"
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }                   // Cột "Đơn giá"
    public decimal DiscountedPrice { get; set; }             // Cột "Đơn giá giảm"
    public decimal LineTotal { get; set; }                   // Cột "Thành tiền"
}
