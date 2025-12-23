using Amazon.S3;
using Amazon.S3.Model;
using Clbio.Abstractions.Interfaces.Infrastructure;
using Clbio.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clbio.Infrastructure.Services
{
    public class S3FileStorageService(
        IAmazonS3 s3Client,
        IOptions<AwsSettings> settings,
        ILogger<S3FileStorageService> logger) : IFileStorageService
    {
        private readonly AwsSettings _settings = settings.Value;

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folderPath, CancellationToken ct = default)
        {
            try
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var cleanFolderPath = folderPath.Trim('/');
                var key = $"{cleanFolderPath}/{uniqueFileName}";

                var request = new PutObjectRequest
                {
                    BucketName = _settings.BucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType,
                    AutoCloseStream = false
                };

                await s3Client.PutObjectAsync(request, ct);

                return GetPresignedUrl(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "S3 Upload failed: {FileName}", fileName);
                throw;
            }
        }

        public string GetPresignedUrl(string key, int durationMinutes = 60)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(durationMinutes)
            };

            return s3Client.GetPreSignedURL(request);
        }

        public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        {
            try
            {
                var key = ExtractKeyFromUrl(fileUrl);
                if (string.IsNullOrEmpty(key)) return;

                await s3Client.DeleteObjectAsync(_settings.BucketName, key, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "S3 Delete failed: {Url}", fileUrl);
                throw;
            }
        }

        private string? ExtractKeyFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            if (!string.IsNullOrEmpty(_settings.PublicUrl) && url.StartsWith(_settings.PublicUrl))
            {
                var parts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
                return parts.Length > 1 ? parts[1] : null;
            }

            var path = uri.AbsolutePath.TrimStart('/');
            var bucketPrefix = $"{_settings.BucketName}/";
            
            if (path.StartsWith(bucketPrefix))
            {
                return path.Substring(bucketPrefix.Length);
            }

            return path;
        }
    }
}