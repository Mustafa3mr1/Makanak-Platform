using System.Text.RegularExpressions;
using Makanak.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Makanak.Infrastructure.Storage;


public sealed partial class LocalImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".webp" };

    private readonly string _root;
    private readonly string _requestPath;

    public LocalImageStorageService(IOptions<ImageStorageSettings> options)
    {
        var settings = options.Value;
        _root = Path.GetFullPath(settings.RootPath);
        _requestPath = "/" + settings.RequestPath.Trim('/');
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Guid propertyId, Stream content, string extension)
    {
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Extension is not allowed.", nameof(extension));

        var folder = propertyId.ToString("N");
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var directory = Path.GetFullPath(Path.Combine(_root, folder));
        EnsureInsideRoot(directory);
        Directory.CreateDirectory(directory);

        var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));
        EnsureInsideRoot(fullPath);

        await using (var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            if (content.CanSeek) content.Position = 0;
            await content.CopyToAsync(file);
        }

        return $"{_requestPath}/{folder}/{fileName}";
    }

    public Task DeleteAsync(string imageUrl)
    {
        var prefix = _requestPath + "/";
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith(prefix, StringComparison.Ordinal))
            return Task.CompletedTask;

        var parts = imageUrl[prefix.Length..].Split('/');
        if (parts.Length != 2 || !FolderPattern().IsMatch(parts[0]) || !FilePattern().IsMatch(parts[1]))
            return Task.CompletedTask; // unknown / unsafe shape: ignore

        var fullPath = Path.GetFullPath(Path.Combine(_root, parts[0], parts[1]));
        EnsureInsideRoot(fullPath);

        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private void EnsureInsideRoot(string fullPath)
    {
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved path escapes the image storage root.");
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex FolderPattern();

    [GeneratedRegex(@"^[0-9a-f]{32}\.(jpg|png|webp)$")]
    private static partial Regex FilePattern();
}
