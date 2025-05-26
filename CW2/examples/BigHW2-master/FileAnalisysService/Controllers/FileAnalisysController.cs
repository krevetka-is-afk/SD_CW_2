using FileAnalisysService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using FileAnalisysService.Services;
using System.Text.Json;
using System.Text;

namespace FileAnalisysService.Controllers
{
    [ApiController]
    [Route("internal/[controller]")]
    public class FileAnalisysController : ControllerBase
    {
        private readonly IFileAnalysisService _fileAnalysisService;

        public FileAnalisysController(IFileAnalysisService fileAnalysisService)
        {
            _fileAnalysisService = fileAnalysisService;
        }

        [HttpPost("analysis/{fileId}")]
        public async Task<IActionResult> AnalyzeFile(Guid fileId)
        {
            try
            {
                FileAnalysisResult existing;
                try
                {
                    existing = await _fileAnalysisService.GetAnalysisResultAsync(fileId);
                }
                catch
                {
                    return StatusCode(503, "Database unavailable");
                }

                if (existing != null)
                    return Ok(existing);

                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"http://filestoringservice:8080/internal/filestoring/{fileId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("File not found");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Error getting file from FileStoringService");

                var content = await response.Content.ReadAsStringAsync();

                var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
                var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                var chars = content.Length;

                var wordCloudRequest = new
                {
                    format = "png",
                    width = 500,
                    height = 500,
                    fontScale = 15,
                    scale = "linear",
                    text = content
                };

                var json = JsonSerializer.Serialize(wordCloudRequest);
                var wordCloudResponse = await httpClient.PostAsync(
                    "https://quickchart.io/wordcloud",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );

                if (!wordCloudResponse.IsSuccessStatusCode)
                    return StatusCode((int)wordCloudResponse.StatusCode, "Failed to generate word cloud");

                var imageBytes = await wordCloudResponse.Content.ReadAsByteArrayAsync();
                var wordCloudFolder = "/app/wordclouds";
                var imagePath = Path.Combine(wordCloudFolder, $"{fileId}.png");

                Directory.CreateDirectory(wordCloudFolder);
                await System.IO.File.WriteAllBytesAsync(imagePath, imageBytes);

                var analysisResult = new FileAnalysisResult
                {
                    Id = fileId,
                    ParagraphCount = paragraphs,
                    WordCount = words,
                    CharacterCount = chars,
                    ImageLocation = imagePath
                };

                try
                {
                    await _fileAnalysisService.SaveAnalysisResultAsync(analysisResult);
                }
                catch
                {
                    return StatusCode(503, "Database unavailable");
                }

                return Ok(analysisResult);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "FileStoringService or word cloud service is unavailable");
            }
            catch (Exception)
            {
                return StatusCode(500, "Unexpected error occurred");
            }
        }

        [HttpGet("wordcloud/{fileId}")]
        public async Task<IActionResult> GetWordCloudById(Guid fileId)
        {
            FileAnalysisResult result;
            try
            {
                result = await _fileAnalysisService.GetAnalysisResultAsync(fileId);
            }
            catch
            {
                return StatusCode(503, "Database unavailable");
            }

            if (result == null)
                return NotFound("Analysis not found");

            if (string.IsNullOrEmpty(result.ImageLocation) || !System.IO.File.Exists(result.ImageLocation))
                return NotFound("Word cloud image not found");

            try
            {
                var imageBytes = await System.IO.File.ReadAllBytesAsync(result.ImageLocation);
                return File(imageBytes, "image/png");
            }
            catch
            {
                return StatusCode(500, "Could not read image");
            }
        }
    }
}
