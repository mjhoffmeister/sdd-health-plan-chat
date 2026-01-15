using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthPlanChat.Infrastructure.Storage;

/// <summary>
/// Helper for uploading plan materials to Azure Blob Storage.
/// </summary>
public sealed class PlanMaterialBlobPublisher
{
    private readonly ILogger<PlanMaterialBlobPublisher> _logger;
    private readonly StorageOptions _options;
    private readonly Lazy<BlobContainerClient> _containerClient;

    public PlanMaterialBlobPublisher(
        IOptions<StorageOptions> options,
        ILogger<PlanMaterialBlobPublisher> logger)
    {
        _logger = logger;
        _options = options.Value;

        _containerClient = new Lazy<BlobContainerClient>(() =>
        {
            var serviceClient = new BlobServiceClient(
                new Uri(_options.BlobServiceUrl),
                new DefaultAzureCredential());
            return serviceClient.GetBlobContainerClient(_options.ContainerName);
        });
    }

    /// <summary>
    /// Uploads a JSON file to blob storage.
    /// </summary>
    /// <param name="fileName">Name of the file (without path).</param>
    /// <param name="content">JSON content to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UploadJsonAsync(
        string fileName,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var blobClient = _containerClient.Value.GetBlobClient(fileName);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/json"
                }
            },
            cancellationToken);

        _logger.LogInformation(
            "Uploaded blob. FileName: {FileName}, SizeBytes: {SizeBytes}",
            fileName,
            content.Length);
    }

    /// <summary>
    /// Uploads a plan material JSON document.
    /// </summary>
    /// <param name="planDocument">The plan document to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UploadPlanDocumentAsync<T>(
        T planDocument,
        string fileName,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var json = JsonSerializer.Serialize(planDocument, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await UploadJsonAsync(fileName, json, cancellationToken);
    }

    /// <summary>
    /// Uploads all JSON files from a local directory.
    /// </summary>
    /// <param name="localDirectory">Path to local directory containing JSON files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of files uploaded.</returns>
    public async Task<int> UploadDirectoryAsync(
        string localDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localDirectory);

        if (!Directory.Exists(localDirectory))
        {
            _logger.LogWarning("Directory does not exist. Path: {Path}", localDirectory);
            return 0;
        }

        var jsonFiles = Directory.GetFiles(localDirectory, "*.json");
        var uploadCount = 0;

        foreach (var filePath in jsonFiles)
        {
            var fileName = Path.GetFileName(filePath);
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);

            await UploadJsonAsync(fileName, content, cancellationToken);
            uploadCount++;
        }

        _logger.LogInformation(
            "Uploaded directory. Directory: {Directory}, FileCount: {FileCount}",
            localDirectory,
            uploadCount);

        return uploadCount;
    }

    /// <summary>
    /// Lists all blobs in the container.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of blob names.</returns>
    public async Task<IReadOnlyList<string>> ListBlobsAsync(CancellationToken cancellationToken = default)
    {
        var blobs = new List<string>();

        await foreach (var blob in _containerClient.Value.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            blobs.Add(blob.Name);
        }

        return blobs.AsReadOnly();
    }

    /// <summary>
    /// Downloads a blob's content as a string.
    /// </summary>
    /// <param name="blobName">Name of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Blob content as string.</returns>
    public async Task<string?> DownloadBlobAsync(
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var blobClient = _containerClient.Value.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Blob does not exist. BlobName: {BlobName}", blobName);
            return null;
        }

        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToString();
    }
}
