using Microsoft.EntityFrameworkCore;
using Product.API.Contracts;
using Product.API.Data;
using Product.API.Entities;

namespace Product.API.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
        Task<bool> UpdateAsync(int id, UpdateCategoryDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly ProductDbContext _context;

        public CategoryService(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryDto(c.CategoryId, c.Name, c.Description))
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return null;
            
            return new CategoryDto(category.CategoryId, category.Name, category.Description);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return new CategoryDto(category.CategoryId, category.Name, category.Description);
        }

        public async Task<bool> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            category.Name = dto.Name;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            // Check if any products use this category
            var hasProducts = await _context.ProductItems.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
                throw new InvalidOperationException("Cannot delete a category that contains products.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
