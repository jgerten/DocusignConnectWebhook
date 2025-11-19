namespace DocuSignWebhook.Application.Interfaces.Services;

/// <summary>
/// Service for interacting with MinIO object storage
/// </summary>
public interface IMinioStorageService
{
    /// <summary>
    /// Uploads a file to MinIO
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="objectKey">Object key/path in bucket</param>
    /// <param name="data">File data</param>
    /// <param name="contentType">MIME content type</param>
    /// <returns>URL or identifier of uploaded object</returns>
    Task<string> UploadFileAsync(string bucketName, string objectKey, byte[] data, string contentType);

    /// <summary>
    /// Uploads a file to MinIO from a stream
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="objectKey">Object key/path in bucket</param>
    /// <param name="stream">Data stream</param>
    /// <param name="contentType">MIME content type</param>
    /// <returns>URL or identifier of uploaded object</returns>
    Task<string> UploadFileAsync(string bucketName, string objectKey, Stream stream, string contentType);

    /// <summary>
    /// Downloads a file from MinIO
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="objectKey">Object key/path in bucket</param>
    /// <returns>File data as byte array</returns>
    Task<byte[]> DownloadFileAsync(string bucketName, string objectKey);

    /// <summary>
    /// Checks if a bucket exists, creates it if not
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    Task EnsureBucketExistsAsync(string bucketName);

    /// <summary>
    /// Deletes a file from MinIO
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="objectKey">Object key/path in bucket</param>
    Task DeleteFileAsync(string bucketName, string objectKey);

    /// <summary>
    /// Gets a presigned URL for temporary access to a file
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="objectKey">Object key/path in bucket</param>
    /// <param name="expiryInSeconds">URL expiry time in seconds (default 3600)</param>
    /// <returns>Presigned URL</returns>
    Task<string> GetPresignedUrlAsync(string bucketName, string objectKey, int expiryInSeconds = 3600);
}
