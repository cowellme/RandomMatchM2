using Microsoft.EntityFrameworkCore;
using RandomMatch.Server.Models;

namespace RandomMatch.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TUser> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TUser>(entity =>
            {
                entity.HasKey(u => u.ChatId);
                entity.Property(u => u.Username).HasMaxLength(100);
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
