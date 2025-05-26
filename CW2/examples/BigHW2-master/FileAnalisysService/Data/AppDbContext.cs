using FileAnalisysService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileAnalisysService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FileAnalysisResult> FileAnalyses { get; set; }
    }

}
