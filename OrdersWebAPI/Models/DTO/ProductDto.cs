namespace OrdersWebAPI.Models.DTO
{
    // DTO básico para Product
    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string? Package { get; set; }
        public bool IsDiscontinued { get; set; }
    }
}
