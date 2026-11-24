using Microsoft.AspNetCore.Hosting;
using Makanak.Application.Common.Interfaces;

namespace Makanak.Infrastructure.Services;

public sealed class LocalDiskFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;

    public LocalDiskFileStorageService(IWebHostEnvironment environment)
    {
        _webRootPath = !string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? environment.WebRootPath
            : Path.Combine(AppContext.BaseDirectory, "wwwroot");

        if (!Directory.Exists(_webRootPath))
        {
            Directory.CreateDirectory(_webRootPath);
        }
    }

    public async Task<string> SaveFileAsync(
        Stream stream,
        string extension,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        var targetDir = Path.Combine(_webRootPath, folderPath.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(targetDir, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var normalizedFolder = folderPath.Trim('/').Replace('\\', '/');
        return $"/{normalizedFolder}/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string relativeFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return Task.CompletedTask;

        var cleanPath = relativeFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_webRootPath, cleanPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
