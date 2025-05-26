using System;

namespace FileStoringService.Models;

public class FileRecord
{
    public Guid FileId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
} 