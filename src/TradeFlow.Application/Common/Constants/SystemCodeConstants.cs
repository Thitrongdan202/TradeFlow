namespace TradeFlow.Application.Common.Constants;

public static class SystemCodeConstants
{
    public const string Product = "Product";
    public const string Customer = "Customer";
    public const string Supplier = "Supplier";
    public const string Warehouse = "Warehouse";
    public const string ProductCategory = "ProductCategory";
    public const string UnitOfMeasure = "UnitOfMeasure";

    public static readonly IReadOnlyDictionary<string, (string Prefix, string Description)> Defaults =
        new Dictionary<string, (string Prefix, string Description)>
        {
            [Product] = ("SP", "Mã sản phẩm hệ thống"),
            [Customer] = ("KH", "Mã khách hàng hệ thống"),
            [Supplier] = ("NCC", "Mã nhà cung cấp hệ thống"),
            [Warehouse] = ("KHO", "Mã kho hàng hệ thống"),
            [ProductCategory] = ("DM", "Mã danh mục sản phẩm hệ thống"),
            [UnitOfMeasure] = ("DV", "Mã đơn vị tính hệ thống"),
        };
}