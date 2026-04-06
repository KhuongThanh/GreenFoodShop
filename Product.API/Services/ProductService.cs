using Microsoft.EntityFrameworkCore;
using Product.API.Contracts;
using Product.API.Data;
using Product.API.Entities;

namespace Product.API.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductItemDto>> GetAllAsync();
        Task<ProductItemDto?> GetByIdAsync(int id);
        Task<ProductItemDto> CreateAsync(CreateProductItemDto dto);
        Task<bool> UpdateAsync(int id, UpdateProductItemDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class ProductService : IProductService
    {
        private readonly ProductDbContext _context;

        public ProductService(ProductDbContext context)
        {
            _context = context;
        }

        private static ProductItemDto MapToDto(ProductItem p)
        {
            return new ProductItemDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Unit = p.Unit,
                NutritionalInfo = p.NutritionalInfo,
                Origin = p.Origin,
                ExpirationDate = p.ExpirationDate,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty
            };
        }

        public async Task<IEnumerable<ProductItemDto>> GetAllAsync()
        {
            var products = await _context.ProductItems
                .Include(p => p.Category)
                .ToListAsync();

            return products.Select(MapToDto);
        }

        public async Task<ProductItemDto?> GetByIdAsync(int id)
        {
            var product = await _context.ProductItems
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return null;
            return MapToDto(product);
        }

        public async Task<ProductItemDto> CreateAsync(CreateProductItemDto dto)
        {
            var product = new ProductItem
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ImageUrl = dto.ImageUrl,
                Unit = dto.Unit,
                NutritionalInfo = dto.NutritionalInfo,
                Origin = dto.Origin,
                ExpirationDate = dto.ExpirationDate,
                CategoryId = dto.CategoryId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductItems.Add(product);
            await _context.SaveChangesAsync();

            // Load category for immediate return
            await _context.Entry(product).Reference(p => p.Category).LoadAsync();

            return MapToDto(product);
        }

        public async Task<bool> UpdateAsync(int id, UpdateProductItemDto dto)
        {
            var product = await _context.ProductItems.FindAsync(id);
            if (product == null) return false;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.ImageUrl = dto.ImageUrl;
            product.Unit = dto.Unit;
            product.NutritionalInfo = dto.NutritionalInfo;
            product.Origin = dto.Origin;
            product.ExpirationDate = dto.ExpirationDate;
            product.CategoryId = dto.CategoryId;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.ProductItems.FindAsync(id);
            if (product == null) return false;

            _context.ProductItems.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
