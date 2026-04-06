using Microsoft.EntityFrameworkCore;
using Product.API.Entities;

namespace Product.API.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductItem> ProductItems => Set<ProductItem>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Category>(e =>
            {
                e.HasKey(x => x.CategoryId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(100);
                e.Property(x => x.Description).HasMaxLength(500);
            });

            b.Entity<ProductItem>(e =>
            {
                e.HasKey(x => x.ProductId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");
                
                e.Property(x => x.Unit).HasMaxLength(50);
                e.Property(x => x.NutritionalInfo).HasMaxLength(1000);
                e.Property(x => x.Origin).HasMaxLength(200);
                
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(x => x.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict); // Better to prevent category deletion if products exist
            });
        }
    }
}
