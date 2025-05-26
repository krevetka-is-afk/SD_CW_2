using FileAnalisysService.Data;
using FileAnalisysService.Models;

namespace FileAnalisysService.Services
{
    public class FileAnalysisService : IFileAnalysisService
    {
        private readonly AppDbContext _context;

        public FileAnalysisService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FileAnalysisResult?> GetAnalysisResultAsync(Guid fileId)
        {
            return await _context.FileAnalyses.FindAsync(fileId);
        }

        public async Task SaveAnalysisResultAsync(FileAnalysisResult result)
        {
            _context.FileAnalyses.Add(result);
            await _context.SaveChangesAsync();
        }
    }
}
