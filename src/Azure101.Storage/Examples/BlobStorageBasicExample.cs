using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Azure101.Storage.Examples;

/// <summary>
/// Demonstrates basic Azure Blob Storage operations
/// Including upload, download, delete, and listing
/// </summary>
public class BlobStorageBasicExample
{
    private readonly ILogger<BlobStorageBasicExample> _logger;
    private readonly BlobContainerClient _containerClient;

    public BlobStorageBasicExample(
        ILogger<BlobStorageBasicExample> logger,
        BlobContainerClient containerClient)
    {
        _logger = logger;
        _containerClient = containerClient;
    }

    /// <summary>
    /// Creates a new blob container
    /// Azure Blob Storage containers are analogous to folders
    /// </summary>
    public async Task CreateContainerAsync(string containerName)
    {
        try
        {
            var containerClient = new BlobContainerClient(
                _containerClient.Uri.GetLeftPart(System.UriPartial.Authority) + "/" + containerName,
                new DefaultAzureCredential());

            var result = await containerClient.CreateAsync();
            _logger.LogInformation("Container '{ContainerName}' created", containerName);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogWarning("Container '{ContainerName}' already exists", containerName);
        }
    }

    /// <summary>
    /// Uploads a blob with the specified content
    /// Overwrites if blob already exists
    /// </summary>
    public async Task UploadBlobAsync(string blobName, string content)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Uploading blob '{BlobName}'", blobName);

            await blobClient.UploadAsync(
                BinaryData.FromString(content),
                overwrite: true);

            _logger.LogInformation("Blob '{BlobName}' uploaded successfully", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob '{BlobName}'", blobName);
            throw;
        }
    }

    /// <summary>
    /// Uploads a blob from a file path
    /// </summary>
    public async Task UploadBlobFromFileAsync(string blobName, string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var blobClient = _containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Uploading blob '{BlobName}' from file '{FilePath}'",
                blobName, filePath);

            await blobClient.UploadAsync(filePath, overwrite: true);

            _logger.LogInformation("Blob '{BlobName}' uploaded successfully from file", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob from file '{FilePath}'", filePath);
            throw;
        }
    }

    /// <summary>
    /// Downloads a blob and returns its content as string
    /// </summary>
    public async Task<string> DownloadBlobAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Downloading blob '{BlobName}'", blobName);

            BlobDownloadInfo download = await blobClient.DownloadAsync();

            using (var streamReader = new StreamReader(download.Content))
            {
                var content = await streamReader.ReadToEndAsync();
                _logger.LogInformation("Blob '{BlobName}' downloaded successfully", blobName);
                return content;
            }
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Blob '{BlobName}' not found", blobName);
            throw new FileNotFoundException($"Blob not found: {blobName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob '{BlobName}'", blobName);
            throw;
        }
    }

    /// <summary>
    /// Downloads a blob and saves it to a file
    /// </summary>
    public async Task DownloadBlobToFileAsync(string blobName, string localFilePath)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Downloading blob '{BlobName}' to file '{FilePath}'",
                blobName, localFilePath);

            BlobDownloadInfo download = await blobClient.DownloadAsync();

            using (var fileStream = File.Create(localFilePath))
            {
                await download.Content.CopyToAsync(fileStream);
            }

            _logger.LogInformation("Blob saved to '{FilePath}'", localFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob to file");
            throw;
        }
    }

    /// <summary>
    /// Deletes a blob
    /// Does not throw if blob doesn't exist
    /// </summary>
    public async Task DeleteBlobAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Deleting blob '{BlobName}'", blobName);

            await blobClient.DeleteAsync();

            _logger.LogInformation("Blob '{BlobName}' deleted successfully", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob '{BlobName}'", blobName);
            throw;
        }
    }

    /// <summary>
    /// Lists all blobs in the container
    /// </summary>
    public async Task<List<string>> ListBlobsAsync()
    {
        var blobs = new List<string>();

        try
        {
            _logger.LogInformation("Listing blobs in container");

            // BlobContainerClient.GetBlobsAsync() returns an async enumerable
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
                _logger.LogDebug("Found blob: {BlobName} (Size: {Size} bytes)",
                    blobItem.Name, blobItem.Properties.ContentLength);
            }

            _logger.LogInformation("Found {BlobCount} blobs", blobs.Count);
            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list blobs");
            throw;
        }
    }

    /// <summary>
    /// Gets metadata about a blob (size, content type, etc.)
    /// </summary>
    public async Task<BlobProperties> GetBlobPropertiesAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Retrieving properties for blob '{BlobName}'", blobName);

            BlobProperties properties = await blobClient.GetPropertiesAsync();

            _logger.LogInformation("Blob size: {Size} bytes, Content-Type: {ContentType}",
                properties.ContentLength, properties.ContentType);

            return properties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get blob properties");
            throw;
        }
    }

    /// <summary>
    /// Checks if a blob exists in the container
    /// </summary>
    public async Task<bool> BlobExistsAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync();

            _logger.LogDebug("Blob '{BlobName}' exists: {Exists}", blobName, exists.Value);

            return exists.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if blob exists");
            throw;
        }
    }
}
