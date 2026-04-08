using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Senda.Core.Interfaces;

namespace Senda.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly string _localRootPath;
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;

    public FileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _localRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage");
        
        var connectionString = _configuration["Storage:AzureBlob:ConnectionString"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        
        _containerName = _configuration["Storage:AzureBlob:ContainerName"] ?? "senda-docs";
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid tenantId)
    {
        if (_blobServiceClient != null)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();
            
            var blobPath = $"{tenantId}/{Guid.NewGuid()}-{fileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);
            
            await blobClient.UploadAsync(fileStream, true);
            return $"azure://{_containerName}/{blobPath}";
        }
        else
        {
            var tenantPath = Path.Combine(_localRootPath, tenantId.ToString());
            if (!Directory.Exists(tenantPath))
            {
                Directory.CreateDirectory(tenantPath);
            }

            var filePath = Path.Combine(tenantPath, $"{Guid.NewGuid()}-{fileName}");
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }
            return filePath;
        }
    }

    public async Task<Stream> GetFileAsync(string filePath)
    {
        if (filePath.StartsWith("azure://"))
        {
            if (_blobServiceClient == null) throw new InvalidOperationException("Azure Blob Storage is not configured.");
            
            var parts = filePath.Replace("azure://", "").Split('/', 2);
            var container = parts[0];
            var blobName = parts[1];
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }
        else
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (filePath.StartsWith("azure://"))
        {
            if (_blobServiceClient == null) return;
            
            var parts = filePath.Replace("azure://", "").Split('/', 2);
            var container = parts[0];
            var blobName = parts[1];
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            await blobClient.DeleteIfExistsAsync();
        }
        else
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
