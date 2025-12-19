using Microsoft.AspNetCore.Http;

namespace Clbio.Application.Helpers
{
    public static class FileValidationHelper
    {
        // İzin verilen resim türlerinin Magic Number'ları
        private static readonly Dictionary<string, List<byte[]>> _imageSignatures = new()
        {
            { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 }, new byte[] { 0x57, 0x45, 0x42, 0x50 } } }
        };

        public static bool IsImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_imageSignatures.ContainsKey(ext)) return false;

            if (!file.ContentType.StartsWith("image/")) return false;

            // magic number check
            try
            {
                using var reader = new BinaryReader(file.OpenReadStream());
                var header = reader.ReadBytes(12);

                var signatures = _imageSignatures[ext];

                foreach (var signature in signatures)
                {
                    if (signature.Length == 4 && ext == ".webp")
                    {
                        // WebP check: starts with RIFF (0-3), has WEBP at (8-11)
                        if (header.Length >= 12 &&
                            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                        {
                            return true;
                        }
                    }
                    else if (header.Take(signature.Length).SequenceEqual(signature))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}