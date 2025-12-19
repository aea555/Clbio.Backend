namespace Clbio.Infrastructure.Options
{
    public class AwsSettings
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = "us-east-1";
        public string? ServiceUrl { get; set; } // Local MinIO 
        public string? PublicUrl { get; set; }  // Local MinIO Public access
    }
}