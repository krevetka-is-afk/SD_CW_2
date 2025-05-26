namespace FileAnalisysService.Models
{
    public class FileAnalysisResult
    {
        public Guid Id { get; set; }
        public int ParagraphCount { get; set; }
        public int WordCount { get; set; }
        public int CharacterCount { get; set; }
        public string? ImageLocation { get; set; }
    }
}
