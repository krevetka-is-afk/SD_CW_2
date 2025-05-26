using FileStoringService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileStoringService.Controllers;

[ApiController]
[Route("internal/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileStorageService fileStorageService,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .txt files are allowed");

        try
        {
            var fileId = await _fileStorageService.StoreFileAsync(file);
            return Ok(fileId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing file");
            return StatusCode(500, "Error storing file");
        }
    }

    [HttpGet("{fileId}")]
    public async Task<IActionResult> GetFile(string fileId)
    {
        if (!Guid.TryParse(fileId, out var guid))
            return BadRequest("Invalid file ID format");

        try
        {
            var fileStream = await _fileStorageService.GetFileAsync(guid);
            if (fileStream == null)
                return NotFound();

            var fileRecord = await _fileStorageService.GetFileRecordAsync(guid);
            if (fileRecord == null)
                return NotFound();

            return File(fileStream, "text/plain", fileRecord.OriginalName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file");
            return StatusCode(500, "Error retrieving file");
        }
    }
} 