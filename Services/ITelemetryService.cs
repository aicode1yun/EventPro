namespace Ticket.Services
{
    public interface ITelemetryService
    {
        Task TrackEventAsync(string name, Dictionary<string, string>? payload = null);
        Task TrackExceptionAsync(string name, string details);
    }
}
