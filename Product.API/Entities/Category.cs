namespace Product.API.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Navigation collection
        public ICollection<ProductItem> Products { get; set; } = new List<ProductItem>();
    }
}
