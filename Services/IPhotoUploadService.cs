namespace Ticket.Services
{
    public interface IPhotoUploadService
    {
        Task<string?> UploadNowOrEnqueueAsync(string localFilePath, string remoteFileName, int? attendeeId = null, string? ticketCode = null, CancellationToken cancellationToken = default);
        Task EnqueueAsync(string localFilePath, string remoteFileName, int? attendeeId = null, string? ticketCode = null);
    }
}
