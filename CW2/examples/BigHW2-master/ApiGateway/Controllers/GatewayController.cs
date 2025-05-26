using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Controllers
{
    [Route("api")]
    [ApiController]
    public class GatewayController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public GatewayController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("default");
        }

        [HttpPost("files/upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File not transferred");

            if (Path.GetExtension(file.FileName)?.ToLower() != ".txt")
                return BadRequest("Only .txt file is allowed");

            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync("http://filestoringservice:8080/internal/filestoring", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return Ok(result);
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "FileStoringService is unavailable");
            }
            catch (Exception)
            {
                return StatusCode(500, "Unexpected error occurred");
            }
        }

        [HttpGet("files/{fileId}")]
        public async Task<IActionResult> GetFile(Guid fileId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://filestoringservice:8080/internal/filestoring/{fileId}");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

                var stream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var contentDisposition = response.Content.Headers.ContentDisposition?.FileName ?? "file.txt";

                return File(stream, contentType, contentDisposition);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "FileStoringService is unavailable");
            }
            catch (Exception)
            {
                return StatusCode(500, "Unexpected error occurred");
            }
        }

        [HttpGet("analysis/{fileId}")]
        public async Task<IActionResult> GetAnalysis(Guid fileId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"http://fileanalisysservice:8080/internal/fileanalisys/analysis/{fileId}", null);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

                var result = await response.Content.ReadAsStringAsync();
                return Ok(result);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "FileAnalisysService is unavailable");
            }
            catch (Exception)
            {
                return StatusCode(500, "Unexpected error occurred");
            }
        }

        [HttpGet("analysis/wordcloud/{fileId}")]
        public async Task<IActionResult> GetWordCloud(Guid fileId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://fileanalisysservice:8080/internal/fileanalisys/wordcloud/{fileId}");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

                var stream = await response.Content.ReadAsStreamAsync();
                return File(stream, "image/png");
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "FileAnalisysService is unavailable");
            }
            catch (Exception)
            {
                return StatusCode(500, "Unexpected error occurred");
            }
        }
    }
}
