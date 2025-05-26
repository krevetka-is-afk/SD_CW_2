using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<FileRecord> Files { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FileRecord>().ToTable("Files");
            modelBuilder.Entity<FileRecord>()
                .HasIndex(f => f.Hash)
                .IsUnique();
        }
    }
}
