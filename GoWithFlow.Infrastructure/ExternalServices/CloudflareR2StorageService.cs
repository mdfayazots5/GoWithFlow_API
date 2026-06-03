using Amazon.S3;
using Amazon.S3.Model;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Infrastructure.ExternalServices;

public sealed class CloudflareR2StorageService : IStorageService
{
    private readonly AmazonS3Client _s3;
    private readonly CloudflareR2Settings _settings;
    private readonly ILogger<CloudflareR2StorageService> _logger;

    public CloudflareR2StorageService(
        IOptions<CloudflareR2Settings> options,
        ILogger<CloudflareR2StorageService> logger)
    {
        _settings = options.Value;
        _logger   = logger;

        _s3 = new AmazonS3Client(
            _settings.AccessKeyId,
            _settings.SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL           = _settings.AccountEndpoint,
                ForcePathStyle       = true,
                SignatureVersion     = "4",
                AuthenticationRegion = "auto"
            });
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string bucketName,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName        = bucketName,
                Key               = objectKey,
                InputStream       = fileStream,
                ContentType       = contentType,
                UseChunkEncoding  = false
            };

            await _s3.PutObjectAsync(request, cancellationToken);

            _logger.LogInformation(
                "R2 upload complete. Bucket={Bucket} Key={Key} ContentType={ContentType}",
                bucketName, objectKey, contentType);

            return objectKey;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "R2 upload failed. Bucket={Bucket} Key={Key} StatusCode={StatusCode}",
                bucketName, objectKey, ex.StatusCode);

            throw new StorageException(bucketName, objectKey, "File upload to storage failed.", ex);
        }
    }

    public Task<string> GetPresignedUrlAsync(
        string bucketName,
        string objectKey,
        int expiryMinutes,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key        = objectKey,
            Expires    = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Verb       = HttpVerb.GET
        };

        var url = _s3.GetPreSignedURL(request);

        _logger.LogInformation(
            "R2 presigned URL generated. Bucket={Bucket} Key={Key} ExpiryMinutes={Expiry}",
            bucketName, objectKey, expiryMinutes);

        return Task.FromResult(url);
    }

    public async Task DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key        = objectKey
            };

            await _s3.DeleteObjectAsync(request, cancellationToken);

            _logger.LogInformation(
                "R2 object deleted. Bucket={Bucket} Key={Key}", bucketName, objectKey);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Object already gone — not an error
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "R2 delete failed. Bucket={Bucket} Key={Key}", bucketName, objectKey);
            throw new StorageException(bucketName, objectKey, "File deletion from storage failed.", ex);
        }
    }

    public async Task<bool> ExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key        = objectKey
            };

            await _s3.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "R2 exists check failed. Bucket={Bucket} Key={Key}", bucketName, objectKey);
            throw new StorageException(bucketName, objectKey, "Storage existence check failed.", ex);
        }
    }
}
