namespace Makanak.Application.Common.Interfaces;

public interface IImageStorageService
{

    Task<string> SaveAsync(Guid propertyId, Stream content, string extension);

    Task DeleteAsync(string imageUrl);
}
