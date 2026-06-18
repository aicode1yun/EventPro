using System.Text.Json;

namespace Ticket.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly string _telemetryFilePath;

        public TelemetryService()
        {
            _telemetryFilePath = Path.Combine(FileSystem.AppDataDirectory, "telemetry.log");
        }

        public async Task TrackEventAsync(string name, Dictionary<string, string>? payload = null)
        {
            try
            {
                var entry = new
                {
                    timestamp = DateTime.UtcNow,
                    type = "event",
                    name,
                    payload = payload ?? new Dictionary<string, string>()
                };

                var json = JsonSerializer.Serialize(entry);
                await File.AppendAllTextAsync(_telemetryFilePath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Telemetry error: {ex.Message}");
            }
        }

        public async Task TrackExceptionAsync(string name, string details)
        {
            try
            {
                var entry = new
                {
                    timestamp = DateTime.UtcNow,
                    type = "exception",
                    name,
                    details
                };

                var json = JsonSerializer.Serialize(entry);
                await File.AppendAllTextAsync(_telemetryFilePath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Telemetry error: {ex.Message}");
            }
        }
    }
}
