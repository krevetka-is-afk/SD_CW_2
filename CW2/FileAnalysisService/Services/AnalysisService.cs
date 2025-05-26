using System.Security.Cryptography;
using System.Text;
using FileAnalysisService.Data;
using FileAnalysisService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Services;

public interface IAnalysisService
{
    Task<AnalysisResult> AnalyzeFileAsync(string fileId, Stream fileContent);
    Task<AnalysisResult?> GetAnalysisResultAsync(string fileId);
    Task<byte[]> GenerateWordCloudAsync(string fileId, Stream fileContent);
}

public class AnalysisService : IAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalysisService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _wordCloudsDirectory;

    public AnalysisService(
        ApplicationDbContext context,
        ILogger<AnalysisService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _wordCloudsDirectory = configuration["WordCloudsDirectory"] ?? "/app/data/wordclouds";
        
        if (!Directory.Exists(_wordCloudsDirectory))
        {
            Directory.CreateDirectory(_wordCloudsDirectory);
        }
    }

    public async Task<AnalysisResult> AnalyzeFileAsync(string fileId, Stream fileContent)
    {
        var text = await new StreamReader(fileContent).ReadToEndAsync();
        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var characters = text.Length;

        using var sha256 = SHA256.Create();
        fileContent.Position = 0;
        var hash = await sha256.ComputeHashAsync(fileContent);
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        var analysisResult = new AnalysisResult
        {
            FileId = Guid.Parse(fileId),
            ParagraphCount = paragraphs.Length,
            WordCount = words.Length,
            CharacterCount = characters,
            Hash = hashString,
            AnalyzedAt = DateTime.UtcNow
        };

        var existingResult = await _context.AnalysisResults.FindAsync(analysisResult.FileId);
        if (existingResult != null)
        {
            _context.Entry(existingResult).CurrentValues.SetValues(analysisResult);
        }
        else
        {
            _context.AnalysisResults.Add(analysisResult);
        }

        await _context.SaveChangesAsync();
        return analysisResult;
    }

    public async Task<AnalysisResult?> GetAnalysisResultAsync(string fileId)
    {
        return await _context.AnalysisResults.FindAsync(Guid.Parse(fileId));
    }

    public async Task<byte[]> GenerateWordCloudAsync(string fileId, Stream fileContent)
    {
        var text = await new StreamReader(fileContent).ReadToEndAsync();
        var words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Select(w => w.ToLowerInvariant())
            .GroupBy(w => w)
            .ToDictionary(g => g.Key, g => g.Count());

        // Format words as comma-separated list with counts
        var wordList = string.Join(",", words.Select(w => $"{w.Key}:{w.Value}"));

        try
        {
            _logger.LogInformation("Generating word cloud for file {FileId} with {WordCount} words", fileId, words.Count);
            using var client = _httpClientFactory.CreateClient();
            
            // Use the wordcloud endpoint with proper parameters
            var response = await client.GetAsync($"https://quickchart.io/wordcloud?text={Uri.EscapeDataString(wordList)}&format=png&width=800&height=600&useWordList=true&cleanWords=false");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to generate word cloud. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to generate word cloud: {errorContent}");
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var filePath = Path.Combine(_wordCloudsDirectory, $"{fileId}.png");
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
            _logger.LogInformation("Word cloud generated and saved to {FilePath}", filePath);

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating word cloud for file {FileId}", fileId);
            throw;
        }
    }
} 