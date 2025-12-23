namespace Clbio.Abstractions.Interfaces.Infrastructure
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Dosyayı storage'a yükler.
        /// </summary>
        /// <param name="fileStream">Dosyanın binary içeriği</param>
        /// <param name="fileName">Orijinal dosya adı (uzantısı için)</param>
        /// <param name="contentType">MIME tipi (image/png vb.)</param>
        /// <param name="folderPath">Klasör yolu (workspaces/1/tasks/5)</param>
        /// <returns>Erişilebilir dosya URL'i</returns>
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folderPath, CancellationToken ct = default);

        /// <summary>
        /// Dosyayı storage'dan siler.
        /// </summary>
        /// <param name="fileUrl">Silinecek dosyanın tam URL'i</param>
        Task DeleteAsync(string fileUrl, CancellationToken ct = default);
        string GetPresignedUrl(string key, int durationMinutes = 60);
    }
}