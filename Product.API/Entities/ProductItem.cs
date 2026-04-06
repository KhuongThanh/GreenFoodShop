namespace Product.API.Entities
{
    public class ProductItem
    {
        public int ProductId { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }

        // Green Food Specific details
        public string? Unit { get; set; } // e.g., kg, gram, bunch, pack
        public string? NutritionalInfo { get; set; }
        public string? Origin { get; set; } // Where the food came from
        public DateTime? ExpirationDate { get; set; }

        // Status
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Key
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
