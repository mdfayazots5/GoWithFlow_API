namespace GoWithFlow.Application.Interfaces.Services;

public interface IStorageService
{
    /// <summary>
    /// Uploads a stream to R2. Returns the object key — store this in the DB, never the URL.
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string bucketName,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited presigned GET URL. Never log the returned URL (contains credentials).
    /// </summary>
    Task<string> GetPresignedUrlAsync(
        string bucketName,
        string objectKey,
        int expiryMinutes,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);
}
