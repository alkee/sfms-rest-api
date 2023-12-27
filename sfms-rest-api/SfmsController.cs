using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using sfms;

namespace sfms_rest_api;

[ApiController]
public class SfmsController
    : ControllerBase
{
    private readonly ILogger<SfmsController> logger;
    private readonly Container container;

    public SfmsController(ILogger<SfmsController> logger, Container container)
    {
        this.logger = logger;
        this.container = container;
    }

    [HttpGet("Version")]
    public IActionResult GetVersion()
    {
        return Ok(versions);
    }

    static SfmsController()
    {
        var assembly = typeof(SfmsController).Assembly;
        var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);

        versions = new
        {
            assemblyVersion = assembly.GetName().Version,
            fileVersion = fvi.FileVersion,
            productVersion = fvi.ProductVersion,
        };
    }

    private static readonly object versions;


    [HttpGet("Files/{**directory=/}")] // https://stackoverflow.com/a/59358713
    public async Task<IActionResult> GetFiles(string directory = "/")
    {
        directory = MakeSureValidPath(directory);
        return Ok(await container.GetFilesAsync(directory));
    }

    [HttpDelete("File/{**filePath}")]
    public async Task<IActionResult> Delete(string filePath)
    {
        filePath = MakeSureValidPath(filePath);
        return Ok(await container.DeleteAsync(filePath));
    }

    [HttpPatch("File/{**filePath}")]
    public async Task<IActionResult> Rename(string filePath, string newFilePath)
    {
        filePath = MakeSureValidPath(filePath);
        newFilePath = MakeSureValidPath(newFilePath);
        return Ok(await container.MoveAsync(filePath, newFilePath));
    }

    [HttpPost("FileContent/{**filePath}")]
    public async Task<IActionResult> WriteFile(string filePath, string originalFileName = "", string base64encodedBytes = "")
    {
        filePath = MakeSureValidPath(filePath, true);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            originalFileName = GetFileName(filePath);
        }

        var decoded = Convert.FromBase64String(base64encodedBytes);
        var stream = new MemoryStream(decoded);
        return Ok(await WriteFile(filePath, originalFileName, stream));
    }


    [HttpPost("Webform/FileContent/{**filePath}")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue /*nolimit*/)]
    public async Task<IActionResult> UploadFile(
        string filePath,
        IFormFile uploadFile)
    {
        var stream = uploadFile.OpenReadStream();
        var file = await WriteFile(filePath, uploadFile.FileName, stream);
        return Ok(file);
    }

    [HttpGet("FileContent/{**filePath}")]
    public async Task<IActionResult> ReadFile(string filePath)
    {
        filePath = MakeSureValidPath(filePath, true);
        var file = await container.GetFileAsync(filePath)
            ?? throw new NotFoundException($"file not found : {filePath}");
        var content = await container.ReadFileAsync(file);
        return Ok(content);
    }

    [HttpGet("Webform/FileContent/{**filePath}")]
    public async Task<IActionResult> DownloadFile(string filePath, bool asOriginalFileName = true)
    {
        filePath = MakeSureValidPath(filePath, true);
        var file = await container.GetFileAsync(filePath)
            ?? throw new NotFoundException($"file not found : {filePath}");
        var meta = FileMeta.FromJson(file.meta);
        var fileName = string.IsNullOrWhiteSpace(meta.OriginalFileName) && asOriginalFileName == false
            ? GetFileName(filePath)
            : meta.OriginalFileName;
        var content = await container.ReadFileAsync(file);
        // TODO: 확장자에 따른 Mime Type
        return File(
            content.data,
            System.Net.Mime.MediaTypeNames.Application.Octet,
            fileName);
    }

    #region internal helpers

    private async Task<sfms.File> WriteFile(string filePath, string originalFileName, Stream stream)
    { // overwrite not allowed !!!
        filePath = MakeSureValidPath(filePath, true);
        if (container.GetFile(filePath) != null)
            throw new AlreadyExistsException($"file already exists : {filePath}");

        var file = await container.WriteFileAsync(filePath, stream);
        if (string.IsNullOrWhiteSpace(originalFileName) == false)
        {
            ArgumentInvalidFileNameException.Validate(originalFileName, nameof(originalFileName));
            var meta = new FileMeta
            {
                OriginalFileName = HttpUtility.UrlDecode(originalFileName)
            };
            file = await container.SetMetaAsync(filePath, meta.ToJson());
        }
        return file;
    }

    private static string MakeSureValidPath(string path, bool shouldFile = false)
    {
        if (!path.Contains('/'))
            path = HttpUtility.UrlDecode(path);
        if (!path.StartsWith('/'))
            path = $"/{path}";
        if (shouldFile)
            ArgumentInvalidFileNameException.Validate(path, nameof(path));
        return path;
    }

    private static string GetFileName(string path)
    {
        return path.Split('/').Last();
    }

    #endregion
}
