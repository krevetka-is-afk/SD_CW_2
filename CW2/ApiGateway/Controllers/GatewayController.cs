using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GatewayController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("files/upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .txt files are allowed");

        try
        {
            using var client = _httpClientFactory.CreateClient();
            var fileStoringUrl = _configuration["FILE_STORING_URL"] ?? "http://file-storing-service";
            
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(fileContent, "file", file.FileName);

            var response = await client.PostAsync($"{fileStoringUrl}/internal/files", content);
            response.EnsureSuccessStatusCode();
            
            var fileId = await response.Content.ReadAsStringAsync();
            return Ok(new { fileId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "Error processing file upload");
        }
    }

    [HttpGet("files/{fileId}")]
    public async Task<IActionResult> GetFile(string fileId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var fileStoringUrl = _configuration["FILE_STORING_URL"] ?? "http://file-storing-service";
            
            var response = await client.GetAsync($"{fileStoringUrl}/internal/files/{fileId}");
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var content = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain";
            var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "file.txt";

            return File(content, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file");
            return StatusCode(500, "Error retrieving file");
        }
    }

    [HttpGet("analysis/{fileId}")]
    public async Task<IActionResult> GetAnalysis(string fileId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var analysisUrl = _configuration["FILE_ANALYSIS_URL"] ?? "http://file-analysis-service";
            
            var response = await client.GetAsync($"{analysisUrl}/internal/analysis/{fileId}");
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis");
            return StatusCode(500, "Error getting analysis");
        }
    }

    [HttpGet("analysis/wordcloud/{fileId}")]
    public async Task<IActionResult> GetWordCloud(string fileId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var analysisUrl = _configuration["FILE_ANALYSIS_URL"] ?? "http://file-analysis-service";
            
            var response = await client.GetAsync($"{analysisUrl}/internal/analysis/{fileId}/wordcloud");
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var content = await response.Content.ReadAsStreamAsync();
            return File(content, "image/png", $"wordcloud_{fileId}.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating word cloud");
            return StatusCode(500, "Error generating word cloud");
        }
    }
} 