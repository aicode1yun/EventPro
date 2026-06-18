namespace Ticket.Services
{
    public interface IPhotoCompressionService
    {
        Task<string?> CompressPhotoAsync(string sourceFilePath, string destinationFileName, int maxWidth = 1024, int maxHeight = 1024, int quality = 80);
        Task<long> GetFileSizeAsync(string filePath);
    }
}
