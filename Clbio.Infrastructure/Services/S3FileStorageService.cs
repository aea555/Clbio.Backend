using Amazon.S3;
using Amazon.S3.Model;
using Clbio.Abstractions.Interfaces.Infrastructure;
using Clbio.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clbio.Infrastructure.Services
{
    public class S3FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AwsSettings _settings;
        private readonly ILogger<S3FileStorageService> _logger;

        public S3FileStorageService(
            IAmazonS3 s3Client, 
            IOptions<AwsSettings> settings,
            ILogger<S3FileStorageService> logger)
        {
            _s3Client = s3Client;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folderPath, CancellationToken ct = default)
        {
            try
            {
                // make file name unique
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                
                // clear folder structure
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

                await _s3Client.PutObjectAsync(request, ct);

                _logger.LogInformation("File uploaded to S3/MinIO. Key: {Key}", key);

                if (!string.IsNullOrEmpty(_settings.PublicUrl))
                {
                    return $"{_settings.PublicUrl.TrimEnd('/')}/{_settings.BucketName}/{key}";
                }

                return $"https://{_settings.BucketName}.s3.amazonaws.com/{key}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 Upload failed for file: {FileName}", fileName);
                throw; 
            }
        }

        public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        {
            try
            {
                var key = ExtractKeyFromUrl(fileUrl);
                
                if (string.IsNullOrEmpty(key))
                {
                    _logger.LogWarning("Could not extract key from URL: {Url}", fileUrl);
                    return;
                }

                await _s3Client.DeleteObjectAsync(_settings.BucketName, key, ct);
                
                _logger.LogInformation("File deleted from S3/MinIO. Key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 Delete failed for url: {Url}", fileUrl);
                throw;
            }
        }

        // ---------------------------------------------------------
        // PRIVATE HELPER: URL'den Key Çıkarma
        // ---------------------------------------------------------
        private string? ExtractKeyFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) 
                return null;

            // 1. MinIO check
            if (!string.IsNullOrEmpty(_settings.PublicUrl) && url.StartsWith(_settings.PublicUrl))
            {
                var path = uri.AbsolutePath.TrimStart('/'); 
                
                var parts = path.Split('/', 2);
                return parts.Length > 1 ? parts[1] : null;
            }

            // 2. AWS S3 check
            return uri.AbsolutePath.TrimStart('/');
        }
    }
}