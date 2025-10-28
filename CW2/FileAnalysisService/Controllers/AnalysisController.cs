using FileAnalysisService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalysisService.Controllers;

[ApiController]
[Route("internal/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly ILogger<AnalysisController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AnalysisController(
        IAnalysisService analysisService,
        ILogger<AnalysisController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _analysisService = analysisService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("{fileId}")]
    public async Task<IActionResult> GetAnalysis(string fileId)
    {
        try
        {
            var result = await _analysisService.GetAnalysisResultAsync(fileId);
            if (result != null)
            {
                return Ok(result);
            }

            // If no analysis exists, get the file and analyze it
            using var client = _httpClientFactory.CreateClient();
            var fileStoringUrl = _configuration["FILE_STORING_URL"] ?? "http://file-storing-service";
            var response = await client.GetAsync($"{fileStoringUrl}/internal/files/{fileId}");
            
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            using var fileContent = await response.Content.ReadAsStreamAsync();
            result = await _analysisService.AnalyzeFileAsync(fileId, fileContent);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file");
            return StatusCode(500, "Error analyzing file");
        }
    }

    [HttpGet("{fileId}/wordcloud")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWordCloud(string fileId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var fileStoringUrl = _configuration["FILE_STORING_URL"] ?? "http://file-storing-service";
            var response = await client.GetAsync($"{fileStoringUrl}/internal/files/{fileId}");
            
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            using var fileContent = await response.Content.ReadAsStreamAsync();
            var imageBytes = await _analysisService.GenerateWordCloudAsync(fileId, fileContent);
            return File(imageBytes, "image/png", $"wordcloud_{fileId}.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating word cloud");
            return StatusCode(500, "Error generating word cloud");
        }
    }
} 