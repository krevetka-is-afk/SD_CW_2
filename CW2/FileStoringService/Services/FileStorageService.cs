using System.Security.Cryptography;
using FileStoringService.Data;
using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Services;

public interface IFileStorageService
{
    Task<Guid> StoreFileAsync(IFormFile file);
    Task<FileStream?> GetFileAsync(Guid fileId);
    Task<FileRecord?> GetFileRecordAsync(Guid fileId);
}

public class FileStorageService : IFileStorageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _filesDirectory;

    public FileStorageService(
        ApplicationDbContext context,
        ILogger<FileStorageService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _filesDirectory = configuration["FilesDirectory"] ?? "/app/data/files";
        
        if (!Directory.Exists(_filesDirectory))
        {
            Directory.CreateDirectory(_filesDirectory);
        }
    }

    public async Task<Guid> StoreFileAsync(IFormFile file)
    {
        using var sha256 = SHA256.Create();
        using var stream = file.OpenReadStream();
        var hash = await sha256.ComputeHashAsync(stream);
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        // Check if file with same hash exists
        var existingFile = await _context.Files
            .FirstOrDefaultAsync(f => f.Hash == hashString);

        if (existingFile != null)
        {
            return existingFile.FileId;
        }

        // Generate new file ID and store the file
        var fileId = Guid.NewGuid();
        var filePath = Path.Combine(_filesDirectory, $"{fileId}.txt");

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var fileRecord = new FileRecord
        {
            FileId = fileId,
            OriginalName = file.FileName,
            Hash = hashString,
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(fileRecord);
        await _context.SaveChangesAsync();

        return fileId;
    }

    public async Task<FileStream?> GetFileAsync(Guid fileId)
    {
        var fileRecord = await GetFileRecordAsync(fileId);
        if (fileRecord == null)
        {
            return null;
        }

        var filePath = Path.Combine(_filesDirectory, $"{fileId}.txt");
        if (!System.IO.File.Exists(filePath))
        {
            return null;
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    public async Task<FileRecord?> GetFileRecordAsync(Guid fileId)
    {
        return await _context.Files.FindAsync(fileId);
    }
} 