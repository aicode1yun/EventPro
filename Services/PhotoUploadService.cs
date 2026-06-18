using System.Text.Json;

namespace Ticket.Services
{
    public class PhotoUploadService : IPhotoUploadService
    {
        private readonly ISupabaseClient _supabase;
        private readonly string _queueFilePath;
        private Timer? _backgroundWorker;
        private const string QueueFileName = "photo_upload_queue.json";

        [System.Runtime.Serialization.DataContract]
        public class PhotoUploadItem
        {
            public Guid Id { get; set; }
            public string LocalPath { get; set; } = string.Empty;
            public string RemoteFileName { get; set; } = string.Empty;
            public string? RemoteUrl { get; set; }
            public int? AttendeeId { get; set; }
            public string? TicketCode { get; set; }
            public int Attempts { get; set; }
            public DateTime NextAttemptUtc { get; set; }
        }

        public PhotoUploadService(ISupabaseClient supabase)
        {
            _supabase = supabase;
            _queueFilePath = Path.Combine(FileSystem.AppDataDirectory, QueueFileName);

            // Start background worker (5 second interval)
            _backgroundWorker = new Timer(async _ => await ProcessQueueAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public async Task<string?> UploadNowOrEnqueueAsync(string localFilePath, string remoteFileName, int? attendeeId = null, string? ticketCode = null, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(localFilePath))
                return null;

            try
            {
                // Try immediate upload if network is available
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    using var stream = File.OpenRead(localFilePath);
                    var remoteUrl = await _supabase.UploadPhotoAsync(stream, remoteFileName);
                    if (!string.IsNullOrEmpty(remoteUrl))
                    {
                        try
                        {
                            if (attendeeId.HasValue)
                                await _supabase.GetAttendeeByIdAsync(attendeeId.Value);
                        }
                        catch { }

                        return remoteUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Photo upload failed, queueing: {ex.Message}");
            }

            // Enqueue for background retry
            await EnqueueAsync(localFilePath, remoteFileName, attendeeId, ticketCode);
            return null;
        }

        public async Task EnqueueAsync(string localFilePath, string remoteFileName, int? attendeeId = null, string? ticketCode = null)
        {
            var item = new PhotoUploadItem
            {
                Id = Guid.NewGuid(),
                LocalPath = localFilePath,
                RemoteFileName = remoteFileName,
                AttendeeId = attendeeId,
                TicketCode = ticketCode,
                Attempts = 0,
                NextAttemptUtc = DateTime.UtcNow
            };

            var queue = await LoadQueueAsync();
            queue.Add(item);
            await SaveQueueAsync(queue);

            System.Diagnostics.Debug.WriteLine($"Photo queued: {remoteFileName}");
        }

        private async Task ProcessQueueAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return;

            var queue = await LoadQueueAsync();
            var now = DateTime.UtcNow;
            var toProcess = queue.Where(x => x.NextAttemptUtc <= now).ToList();

            foreach (var item in toProcess)
            {
                try
                {
                    if (!File.Exists(item.LocalPath))
                    {
                        queue.Remove(item);
                        continue;
                    }

                    using var stream = File.OpenRead(item.LocalPath);
                    var remoteUrl = await _supabase.UploadPhotoAsync(stream, item.RemoteFileName);

                    if (!string.IsNullOrEmpty(remoteUrl))
                    {
                        item.RemoteUrl = remoteUrl;

                        // Update attendee if we have ID or ticket code
                        if (item.AttendeeId.HasValue)
                        {
                            var attendee = await _supabase.GetAttendeeByIdAsync(item.AttendeeId.Value);
                            if (attendee != null)
                            {
                                attendee.PhotoUrl = remoteUrl;
                                await _supabase.SaveAttendeeAsync(attendee);
                            }
                        }
                        else if (!string.IsNullOrEmpty(item.TicketCode))
                        {
                            var attendee = await _supabase.GetAttendeeByTicketCodeAsync(item.TicketCode);
                            if (attendee != null)
                            {
                                attendee.PhotoUrl = remoteUrl;
                                await _supabase.SaveAttendeeAsync(attendee);
                            }
                        }

                        queue.Remove(item);
                        System.Diagnostics.Debug.WriteLine($"Photo uploaded: {item.RemoteFileName}");
                    }
                    else
                    {
                        item.Attempts++;
                        // Exponential backoff: 2^attempts minutes
                        var backoffMinutes = Math.Pow(2, item.Attempts);
                        item.NextAttemptUtc = DateTime.UtcNow.AddMinutes(backoffMinutes);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Photo upload error: {ex.Message}");
                    item.Attempts++;
                    var backoffMinutes = Math.Pow(2, item.Attempts);
                    item.NextAttemptUtc = DateTime.UtcNow.AddMinutes(backoffMinutes);
                }
            }

            await SaveQueueAsync(queue);
        }

        private async Task<List<PhotoUploadItem>> LoadQueueAsync()
        {
            try
            {
                if (!File.Exists(_queueFilePath))
                    return new();

                var json = await File.ReadAllTextAsync(_queueFilePath);
                return JsonSerializer.Deserialize<List<PhotoUploadItem>>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }

        private async Task SaveQueueAsync(List<PhotoUploadItem> queue)
        {
            try
            {
                var json = JsonSerializer.Serialize(queue);
                await File.WriteAllTextAsync(_queueFilePath, json);
            }
            catch { }
        }
    }
}
