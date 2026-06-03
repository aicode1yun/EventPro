namespace Ticket.Services
{
    public interface IMediaService
    {
        Task<bool> SaveToGalleryAsync(string filePath, string title);
    }
}
