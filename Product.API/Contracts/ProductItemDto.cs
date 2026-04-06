namespace Product.API.Contracts
{
    public class ProductItemDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        
        public string? Unit { get; set; }
        public string? NutritionalInfo { get; set; }
        public string? Origin { get; set; }
        public DateTime? ExpirationDate { get; set; }
        
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CreateProductItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? NutritionalInfo { get; set; }
        public string? Origin { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public int CategoryId { get; set; }
    }

    public class UpdateProductItemDto : CreateProductItemDto
    {
        public bool IsActive { get; set; }
    }
}
