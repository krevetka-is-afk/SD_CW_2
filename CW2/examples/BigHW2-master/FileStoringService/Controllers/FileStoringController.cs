using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using FileStoringService.Data;
using FileStoringService.Models;

namespace FileStoringService.Controllers
{
    [ApiController]
    [Route("internal/[controller]")]
    public class FileStoringController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FileStoringController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("The file was not provided.");

            try
            {
                string hash;
                using (var sha256 = SHA256.Create())
                using (var stream = file.OpenReadStream())
                {
                    var hashBytes = await sha256.ComputeHashAsync(stream);
                    hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

                FileRecord existingFile;
                try
                {
                    existingFile = await _context.Files.FirstOrDefaultAsync(f => f.Hash == hash);
                }
                catch
                {
                    return StatusCode(503, "Database unavailable");
                }

                if (existingFile != null)
                    return Ok(new { id = existingFile.Id });

                var fileId = Guid.NewGuid();
                var originalFileName = Path.GetFileName(file.FileName);
                var uploadsRoot = Path.Combine("/app/uploads");
                Directory.CreateDirectory(uploadsRoot);

                var savedFileName = $"{fileId}_{originalFileName}";
                var location = Path.Combine(uploadsRoot, savedFileName);

                if (!System.IO.File.Exists(location))
                {
                    using (var stream = new FileStream(location, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                var fileRecord = new FileRecord
                {
                    Id = fileId,
                    Name = originalFileName,
                    Hash = hash,
                    Location = location
                };

                try
                {
                    _context.Files.Add(fileRecord);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    return StatusCode(503, "Database unavailable");
                }

                return Ok(new { id = fileId });
            }
            catch
            {
                return StatusCode(500, "Failed to save file");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            FileRecord fileRecord;
            try
            {
                fileRecord = await _context.Files.FindAsync(id);
            }
            catch
            {
                return StatusCode(503, "Database unavailable");
            }

            if (fileRecord == null)
                return NotFound("File not found in database.");

            if (!System.IO.File.Exists(fileRecord.Location))
                return NotFound("The file was not found on disk.");

            try
            {
                var fileStream = new FileStream(fileRecord.Location, FileMode.Open, FileAccess.Read);
                var contentType = "text/plain";
                return File(fileStream, contentType, fileRecord.Name);
            }
            catch
            {
                return StatusCode(500, "Could not read file from disk");
            }
        }
    }
}
