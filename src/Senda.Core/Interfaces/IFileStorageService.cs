namespace Senda.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid tenantId);
    Task<Stream> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
