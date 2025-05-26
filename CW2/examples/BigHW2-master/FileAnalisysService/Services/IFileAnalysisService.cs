using FileAnalisysService.Models;

namespace FileAnalisysService.Services
{
    public interface IFileAnalysisService
    {
        Task<FileAnalysisResult?> GetAnalysisResultAsync(Guid fileId);
        Task SaveAnalysisResultAsync(FileAnalysisResult result);
    }
}
