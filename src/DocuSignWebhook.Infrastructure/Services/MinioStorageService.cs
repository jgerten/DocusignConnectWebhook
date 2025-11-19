using DocuSignWebhook.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace DocuSignWebhook.Infrastructure.Services;

/// <summary>
/// Implementation of MinIO object storage service
/// </summary>
public class MinioStorageService : IMinioStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        ILogger<MinioStorageService> logger,
        string endpoint,
        string accessKey,
        string secretKey,
        bool useSSL = false)
    {
        _logger = logger;

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSSL)
            .Build();
    }

    public async Task EnsureBucketExistsAsync(string bucketName)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);

            var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!exists)
            {
                _logger.LogInformation("Creating MinIO bucket: {BucketName}", bucketName);

                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs);

                _logger.LogInformation("Successfully created bucket: {BucketName}", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", bucketName);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(string bucketName, string objectKey, byte[] data, string contentType)
    {
        using var stream = new MemoryStream(data);
        return await UploadFileAsync(bucketName, objectKey, stream, contentType);
    }

    public async Task<string> UploadFileAsync(string bucketName, string objectKey, Stream stream, string contentType)
    {
        try
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("Successfully uploaded {ObjectKey} to bucket {BucketName}",
                objectKey, bucketName);

            return $"{bucketName}/{objectKey}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {ObjectKey} to bucket {BucketName}",
                objectKey, bucketName);
            throw;
        }
    }

    public async Task<byte[]> DownloadFileAsync(string bucketName, string objectKey)
    {
        try
        {
            using var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(async (stream) =>
                {
                    await stream.CopyToAsync(memoryStream);
                });

            await _minioClient.GetObjectAsync(getObjectArgs);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {ObjectKey} from bucket {BucketName}",
                objectKey, bucketName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectKey)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);

            _logger.LogInformation("Successfully deleted {ObjectKey} from bucket {BucketName}",
                objectKey, bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {ObjectKey} from bucket {BucketName}",
                objectKey, bucketName);
            throw;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string bucketName, string objectKey, int expiryInSeconds = 3600)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithExpiry(expiryInSeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting presigned URL for {ObjectKey} in bucket {BucketName}",
                objectKey, bucketName);
            throw;
        }
    }
}
