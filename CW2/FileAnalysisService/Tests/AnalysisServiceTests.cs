// using System;
// using System.Net;
// using System.Net.Http;
// using System.Text;
// using System.Threading.Tasks;
// using FileAnalysisService.Data;
// using FileAnalysisService.Models;
// using FileAnalysisService.Services;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration;
// using Moq;
// using Xunit;
//
// namespace FileAnalysisService.Tests;
//
// public class AnalysisServiceTests
// {
//     private readonly Mock<ILogger<AnalysisService>> _loggerMock;
//     private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
//     private readonly Mock<IConfiguration> _configurationMock;
//     private readonly ApplicationDbContext _context;
//     private readonly AnalysisService _service;
//
//     public AnalysisServiceTests()
//     {
//         _loggerMock = new Mock<ILogger<AnalysisService>>();
//         _httpClientFactoryMock = new Mock<IHttpClientFactory>();
//         _configurationMock = new Mock<IConfiguration>();
//
//         var options = new DbContextOptionsBuilder<ApplicationDbContext>()
//             .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//             .Options;
//
//         _context = new ApplicationDbContext(options);
//         _service = new AnalysisService(_context, _loggerMock.Object, _httpClientFactoryMock.Object, _configurationMock.Object);
//     }
//
//     [Fact]
//     public async Task AnalyzeFileAsync_ShouldCountWordsAndParagraphsCorrectly()
//     {
//         // Arrange
//         var fileId = Guid.NewGuid().ToString();
//         var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";
//         var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
//
//         // Act
//         var result = await _service.AnalyzeFileAsync(fileId, stream);
//
//         // Assert
//         Assert.Equal(3, result.ParagraphCount);
//         Assert.Equal(6, result.WordCount);
//         Assert.Equal(text.Length, result.CharacterCount);
//     }
//
//     [Fact]
//     public async Task GetAnalysisResultAsync_ShouldReturnNullForNonExistentFile()
//     {
//         // Arrange
//         var fileId = Guid.NewGuid().ToString();
//
//         // Act
//         var result = await _service.GetAnalysisResultAsync(fileId);
//
//         // Assert
//         Assert.Null(result);
//     }
//
//     [Fact]
//     public async Task GetAnalysisResultAsync_ShouldReturnExistingAnalysis()
//     {
//         // Arrange
//         var fileId = Guid.NewGuid();
//         var analysis = new AnalysisResult
//         {
//             FileId = fileId,
//             ParagraphCount = 5,
//             WordCount = 10,
//             CharacterCount = 50,
//             Hash = "test-hash",
//             AnalyzedAt = DateTime.UtcNow
//         };
//
//         _context.AnalysisResults.Add(analysis);
//         await _context.SaveChangesAsync();
//
//         // Act
//         var result = await _service.GetAnalysisResultAsync(fileId.ToString());
//
//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(analysis.ParagraphCount, result.ParagraphCount);
//         Assert.Equal(analysis.WordCount, result.WordCount);
//         Assert.Equal(analysis.CharacterCount, result.CharacterCount);
//     }
//
//     [Fact]
//     public async Task GenerateWordCloudAsync_ShouldGenerateImage()
//     {
//         // Arrange
//         var fileId = Guid.NewGuid().ToString();
//         var text = "This is a test text with some repeated words. This is a test.";
//         var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
//
//         var httpClient = new HttpClient();
//         _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
//             .Returns(httpClient);
//
//         // Act
//         var result = await _service.GenerateWordCloudAsync(fileId, stream);
//
//         // Assert
//         Assert.NotNull(result);
//         Assert.True(result.Length > 0);
//     }
// } 