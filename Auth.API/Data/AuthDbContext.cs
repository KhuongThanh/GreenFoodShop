using Auth.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }


        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<AuthProvider> AuthProviders => Set<AuthProvider>();
        public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<TwoFactorAuth> TwoFactorAuths => Set<TwoFactorAuth>();
        public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<UserVerification> UserVerifications => Set<UserVerification>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);


            b.Entity<User>(e =>
            {
                e.HasKey(x => x.UserId);
                e.HasIndex(x => x.UserName).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.EmailConfirmed).HasDefaultValue(false);
                e.Property(x => x.PhoneConfirmed).HasDefaultValue(false);
                e.Property(x => x.IsExternalAccount).HasDefaultValue(false);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.Property(x => x.IsLocked).HasDefaultValue(false);
                e.Property(x => x.AccessFailedCount).HasDefaultValue(0);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.SecurityStamp).HasDefaultValueSql("NEWID()");
                e.Property(x => x.ConcurrencyStamp).HasDefaultValueSql("NEWID()");

                e.HasCheckConstraint("CK_Users_Lockout",
                "IsLocked = 0 OR (IsLocked = 1 AND LockoutEnd IS NOT NULL)");
            });


            b.Entity<UserProfile>(e =>
            {
                e.HasKey(x => x.ProfileId);
                e.HasIndex(x => x.UserId).IsUnique();
                e.HasOne(x => x.User)
                .WithOne(x => x.Profile)
                .HasForeignKey<UserProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            });


            b.Entity<Role>(e =>
            {
                e.HasKey(x => x.RoleId);
                e.HasIndex(x => x.RoleName).IsUnique();
            });


            b.Entity<UserRole>(e =>
            {
                e.HasKey(x => new { x.UserId, x.RoleId });
            });
            // AuthProviders
            b.Entity<AuthProvider>(e =>
            {
                e.HasKey(x => x.Provider);
                e.Property(x => x.Provider).HasMaxLength(50);
                e.Property(x => x.DisplayName).HasMaxLength(100);
                e.Property(x => x.IsEnabled).HasDefaultValue(true);
            });

            // UserExternalLogins
            b.Entity<UserExternalLogin>(e =>
            {
                e.HasKey(x => x.ExternalLoginId);

                e.Property(x => x.Provider)
                    .HasMaxLength(50)
                    .IsRequired();

                e.Property(x => x.ProviderUserId)
                    .HasMaxLength(255)
                    .IsRequired();

                e.HasOne(x => x.User)
                    .WithMany(u => u.ExternalLogins)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
            });

            // RefreshTokens
            b.Entity<RefreshToken>(e =>
            {
                e.HasKey(x => x.TokenId);

                e.Property(x => x.TokenHash).HasMaxLength(500);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.User)
                 .WithMany(x => x.RefreshTokens)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // TwoFactorAuths
            b.Entity<TwoFactorAuth>(e =>
            {
                e.HasKey(x => x.TwoFactorId);

                e.Property(x => x.Provider).HasMaxLength(50);
                e.Property(x => x.SecretKey).HasMaxLength(500);
                e.Property(x => x.IsEnabled).HasDefaultValue(false);

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // PasswordResets
            b.Entity<PasswordReset>(e =>
            {
                e.HasKey(x => x.ResetId);

                e.Property(x => x.ResetToken).HasMaxLength(500);

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLogs
            b.Entity<AuditLog>(e =>
            {
                e.HasKey(x => x.AuditId);

                e.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");
            });
            //fix
            b.Entity<UserVerification>(e =>
            {
                e.HasKey(x => x.VerificationId);

                e.Property(x => x.Code).HasMaxLength(10);

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });



        }
    }
}