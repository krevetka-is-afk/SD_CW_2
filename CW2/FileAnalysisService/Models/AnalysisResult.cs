using System;

namespace FileAnalysisService.Models;

public class AnalysisResult
{
    public Guid FileId { get; set; }
    public int ParagraphCount { get; set; }
    public int WordCount { get; set; }
    public int CharacterCount { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
} 