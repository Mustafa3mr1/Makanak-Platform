namespace Makanak.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Saves a stream to storage under the given folder path and returns the public relative URL.
    /// </summary>
    Task<string> SaveFileAsync(Stream stream, string extension, string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the file if it exists.
    /// </summary>
    Task DeleteFileAsync(string relativeFilePath, CancellationToken cancellationToken = default);
}
