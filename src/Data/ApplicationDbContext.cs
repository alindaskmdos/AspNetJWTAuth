using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using reg.Models;

namespace reg.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("app_users");
            modelBuilder.Entity<IdentityRole>().ToTable("app_roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("app_user_roles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("app_user_claims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("app_user_logins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("app_role_claims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("app_user_tokens");

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("app_refresh_tokens");
                entity.HasKey(e => e.TokenId);

                entity.HasOne(e => e.User)
                     .WithMany(e => e.RefreshTokens)
                     .HasForeignKey(e => e.UserId)
                     .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
