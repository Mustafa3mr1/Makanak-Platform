namespace Makanak.Infrastructure.Storage;

public class ImageStorageSettings
{
    public const string SectionName = "ImageStorage";

    public string RootPath { get; set; } = "uploads";

    public string RequestPath { get; set; } = "/uploads";
}
