using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<FileRecord> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.HasKey(e => e.FileId);
            entity.Property(e => e.OriginalName).IsRequired();
            entity.Property(e => e.Hash).IsRequired();
            entity.Property(e => e.UploadedAt).IsRequired();
        });
    }
} 