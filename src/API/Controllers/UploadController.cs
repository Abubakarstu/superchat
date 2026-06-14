using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/{fileName}";
        _logger.LogInformation("Uploaded file: {FileName} ({Size} bytes)", fileName, file.Length);

        return Ok(new
        {
            url,
            fileName,
            originalName = file.FileName,
            size = file.Length,
            contentType = file.ContentType
        });
    }

    [HttpPost("base64")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public IActionResult UploadBase64([FromBody] Base64UploadRequest request)
    {
        if (string.IsNullOrEmpty(request.FileData))
            return BadRequest(new { error = "No file data provided" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(request.FileName) ?? ".bin";
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        var bytes = Convert.FromBase64String(request.FileData);
        System.IO.File.WriteAllBytes(filePath, bytes);

        var url = $"/uploads/{fileName}";

        return Ok(new { url, fileName, size = bytes.Length });
    }
}

public class Base64UploadRequest
{
    public string FileData { get; set; } = string.Empty;
    public string FileName { get; set; } = "file.bin";
    public string? ContentType { get; set; }
}
