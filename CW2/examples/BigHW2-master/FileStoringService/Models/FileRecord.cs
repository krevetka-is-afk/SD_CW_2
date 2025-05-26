namespace FileStoringService.Models
{
    public class FileRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Hash { get; set; } = null!;
        public string Location { get; set; } = null!;
    }
}
