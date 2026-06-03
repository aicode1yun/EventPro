using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ticket.Data;
using Ticket.Helpers;
using Ticket.Models;

namespace Ticket.Services
{
    public class SupabaseClient : ISupabaseClient
    {
        private readonly AppDbContext _localDb;
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SupabaseClient(AppDbContext localDb)
        {
            _localDb = localDb;
            _supabaseUrl = SupabaseConfig.Url.TrimEnd('/');
            _anonKey = SupabaseConfig.AnonKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_supabaseUrl),
                DefaultRequestHeaders = { { "apikey", _anonKey } }
            };
        }

        // ====================================================================
        // AUTH
        // ====================================================================

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
        {
            try
            {
                var body = new { email, password, gotrue_meta_security = new { } };
                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/auth/v1/token?grant_type=password", content);
                if (!response.IsSuccessStatusCode)
                    return false;

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("access_token", out var tokenProp))
                    return false;

                var accessToken = tokenProp.GetString()!;
                var refreshToken = root.TryGetProperty("refresh_token", out var rt)
                    ? rt.GetString() ?? ""
                    : "";

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                await SecureStorageHelper.SetSupabaseSessionAsync(accessToken, refreshToken);
                await SecureStorageHelper.SetRememberMeAsync(rememberMe);
                if (rememberMe)
                    await SecureStorageHelper.SetLoggedInAsync(true);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            await SecureStorageHelper.ClearAuthAsync();
        }

        public async Task<bool> IsLoggedInAsync()
        {
            var token = await SecureStorageHelper.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return true;
        }

        // ====================================================================
        // REST HELPERS
        // ====================================================================

        private async Task<T?> GetSingleAsync<T>(string path) where T : class
        {
            try
            {
                var response = await _httpClient.GetAsync(path);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
                return list?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<T>> GetListAsync<T>(string path) where T : class
        {
            try
            {
                var response = await _httpClient.GetAsync(path);
                if (!response.IsSuccessStatusCode) return new();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new();
            }
            catch
            {
                return new();
            }
        }

        private async Task<T?> PostAndReturnAsync<T>(string path, object body) where T : class
        {
            var requestJson = JsonSerializer.Serialize(body, JsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = content
            };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var responseJson = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<T>>(responseJson, JsonOptions);
            return list?.FirstOrDefault();
        }

        private async Task<T?> PatchAndReturnAsync<T>(string path, object body) where T : class
        {
            var requestJson = JsonSerializer.Serialize(body, JsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, path)
            {
                Content = content
            };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var responseJson = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<T>>(responseJson, JsonOptions);
            return list?.FirstOrDefault();
        }

        private async Task<bool> DeleteAsync(string path)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(path);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> UploadFileAsync(string bucket, string fileName, Stream fileStream, string contentType)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"/storage/v1/object/{bucket}/{fileName}")
                {
                    Content = new StreamContent(fileStream)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                return $"{_supabaseUrl}/storage/v1/object/public/{bucket}/{fileName}";
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> DeleteFileAsync(string bucket, string fileName)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"/storage/v1/object/{bucket}/{fileName}");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> UploadPhotoAsync(Stream photoStream, string fileName)
        {
            return await UploadFileAsync("attendee-photos", fileName, photoStream, "image/jpeg");
        }

        public async Task<bool> DeletePhotoAsync(string photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl)) return false;

            var bucket = "attendee-photos";
            var uri = new Uri(photoUrl);
            var fileName = uri.Segments.LastOrDefault();
            if (string.IsNullOrEmpty(fileName)) return false;

            return await DeleteFileAsync(bucket, fileName);
        }

        private async Task<int> GetCountAsync(string path)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, path);
                request.Headers.Add("Prefer", "count=exact");
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode && response.Content.Headers.TryGetValues("content-range", out var ranges))
                {
                    var range = ranges.FirstOrDefault();
                    if (range is not null && range.Contains('/'))
                    {
                        var parts = range.Split('/');
                        if (parts.Length == 2 && int.TryParse(parts[1], out var count))
                            return count;
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        // ====================================================================
        // ATTENDEES
        // ====================================================================

        public async Task<List<Attendee>> GetAttendeesAsync()
        {
            var attendees = await GetListAsync<Attendee>("/rest/v1/attendees?select=*&order=registered_at.desc");
            if (attendees.Count > 0)
            {
                await CacheLocallyAsync(attendees);
                return attendees;
            }

            return await _localDb.GetAttendeesAsync();
        }

        public async Task<List<Attendee>> SearchAttendeesAsync(string query)
        {
            var encoded = Uri.EscapeDataString(query);
            var path = $"/rest/v1/attendees?select=*&or=(full_name.ilike.*{encoded}*,ticket_code.ilike.*{encoded}*,phone_number.ilike.*{encoded}*)&order=registered_at.desc";

            var attendees = await GetListAsync<Attendee>(path);
            if (attendees.Count > 0)
                return attendees;

            return await _localDb.SearchAttendeesAsync(query);
        }

        public async Task<Attendee?> GetAttendeeByTicketCodeAsync(string ticketCode)
        {
            var encoded = Uri.EscapeDataString(ticketCode);
            var attendee = await GetSingleAsync<Attendee>($"/rest/v1/attendees?ticket_code=eq.{encoded}&select=*");
            if (attendee is not null)
            {
                await CacheLocallyAsync(attendee);
                return attendee;
            }

            return await _localDb.GetAttendeeByTicketCodeAsync(ticketCode);
        }

        public async Task<Attendee?> GetAttendeeByIdAsync(int id)
        {
            var attendee = await GetSingleAsync<Attendee>($"/rest/v1/attendees?id=eq.{id}&select=*");
            if (attendee is not null)
            {
                await CacheLocallyAsync(attendee);
                return attendee;
            }

            return await _localDb.GetAttendeeByIdAsync(id);
        }

        public async Task<Attendee?> GetAttendeeByPhoneAsync(string phone)
        {
            var encoded = Uri.EscapeDataString(phone);
            var attendee = await GetSingleAsync<Attendee>($"/rest/v1/attendees?phone_number=eq.{encoded}&select=*");
            if (attendee is not null)
            {
                await CacheLocallyAsync(attendee);
                return attendee;
            }

            return await _localDb.GetAttendeeByPhoneAsync(phone);
        }

        public async Task SaveAttendeeAsync(Attendee attendee)
        {
            var tries = 0;
            while (string.IsNullOrWhiteSpace(attendee.TicketCode))
            {
                attendee.TicketCode = TicketCodeGenerator.GenerateCode();
                tries++;
                if (tries > 10) break;
            }

            if (attendee.Id == 0)
            {
                await InsertAttendeeAsync(attendee);
            }
            else
            {
                await UpdateAttendeeAsync(attendee);
            }
        }

        private async Task InsertAttendeeAsync(Attendee attendee)
        {
            for (int tries = 0; tries < 10; tries++)
            {
                var body = new
                {
                    full_name = attendee.FullName,
                    phone_number = string.IsNullOrEmpty(attendee.PhoneNumber) ? null : attendee.PhoneNumber,
                    ticket_type = attendee.TicketType,
                    ticket_code = attendee.TicketCode,
                    qr_token = attendee.QrToken,
                    is_checked_in = attendee.IsCheckedIn,
                    checked_in_at = attendee.CheckedInAt,
                    registered_at = attendee.RegisteredAt,
                    notes = attendee.Notes,
                    payment_status = attendee.PaymentStatus,
                    photo_url = attendee.PhotoUrl
                };

                var result = await PostAndReturnAsync<SupabaseAttendee>("/rest/v1/attendees", body);
                if (result is not null)
                {
                    attendee.Id = result.Id;
                    await CacheLocallyAsync(attendee);
                    return;
                }

                attendee.TicketCode = TicketCodeGenerator.GenerateCode();
            }

            throw new InvalidOperationException("Failed to create attendee. Please try again.");
        }

        private async Task UpdateAttendeeAsync(Attendee attendee)
        {
            var body = new
            {
                full_name = attendee.FullName,
                phone_number = string.IsNullOrEmpty(attendee.PhoneNumber) ? null : attendee.PhoneNumber,
                ticket_type = attendee.TicketType,
                is_checked_in = attendee.IsCheckedIn,
                checked_in_at = attendee.CheckedInAt,
                    notes = attendee.Notes,
                    payment_status = attendee.PaymentStatus,
                    photo_url = attendee.PhotoUrl
                };

            var result = await PatchAndReturnAsync<SupabaseAttendee>($"/rest/v1/attendees?id=eq.{attendee.Id}", body);
            if (result is not null)
            {
                attendee.Id = result.Id;
            }

            await CacheLocallyAsync(attendee);
        }

        public async Task DeleteAttendeeAsync(Attendee attendee)
        {
            var deleted = await DeleteAsync($"/rest/v1/attendees?id=eq.{attendee.Id}");
            if (deleted)
            {
                if (!string.IsNullOrEmpty(attendee.PhotoUrl))
                {
                    try { await DeletePhotoAsync(attendee.PhotoUrl); }
                    catch { }
                }

                try
                {
                    await _localDb.DeleteAttendeeAsync(attendee);
                }
                catch { }
            }
            else
            {
                throw new InvalidOperationException("Failed to delete attendee. Check your connection.");
            }
        }

        // ====================================================================
        // EVENT
        // ====================================================================

        public async Task<Event?> GetEventAsync()
        {
            var evt = await GetSingleAsync<SupabaseEvent>("/rest/v1/events?select=*&limit=1");
            if (evt is not null)
            {
                var mapped = MapToLocalEvent(evt);
                await CacheLocallyAsync(mapped);
                return mapped;
            }

            return await _localDb.GetEventAsync();
        }

        public async Task SaveEventAsync(Event evt)
        {
            var existing = await GetSingleAsync<SupabaseEvent>("/rest/v1/events?select=*&limit=1");
            var body = new
            {
                event_name = evt.EventName,
                event_date = evt.EventDate,
                venue = evt.Venue,
                description = evt.Description
            };

            if (existing is not null)
            {
                await PatchAndReturnAsync<SupabaseEvent>($"/rest/v1/events?id=eq.{existing.Id}", body);
            }
            else
            {
                var created = await PostAndReturnAsync<SupabaseEvent>("/rest/v1/events", body);
                if (created is not null)
                    evt.Id = created.Id;
            }

            await CacheLocallyAsync(evt);
        }

        // ====================================================================
        // STATS
        // ====================================================================

        public async Task<int> GetTotalAttendeesAsync()
        {
            var count = await GetCountAsync("/rest/v1/attendees?select=id");
            if (count > 0) return count;
            return await _localDb.GetTotalAttendeesAsync();
        }

        public async Task<int> GetCheckedInCountAsync()
        {
            var count = await GetCountAsync("/rest/v1/attendees?select=id&is_checked_in=eq.true");
            if (count > 0) return count;
            return await _localDb.GetCheckedInCountAsync();
        }

        // ====================================================================
        // LOCAL CACHE HELPERS
        // ====================================================================

        private async Task CacheLocallyAsync(List<Attendee> attendees)
        {
            try { await _localDb.SyncAllAttendeesAsync(attendees); }
            catch { /* cache best-effort */ }
        }

        private async Task CacheLocallyAsync(Attendee attendee)
        {
            try { await _localDb.SaveAttendeeAsync(attendee); }
            catch { }
        }

        private async Task CacheLocallyAsync(Event evt)
        {
            try { await _localDb.SaveEventAsync(evt); }
            catch { }
        }

        private static Event MapToLocalEvent(SupabaseEvent se)
        {
            return new Event
            {
                Id = se.Id,
                EventName = se.EventName ?? "EventPro Conference",
                EventDate = se.EventDate ?? DateTime.Today.AddMonths(1),
                Venue = se.Venue ?? "Main Hall",
                Description = se.Description,
                LogoPath = se.LogoPath
            };
        }
    }

    // DTOs that match Supabase snake_case columns exactly
    internal class SupabaseAttendee
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("ticket_type")]
        public string TicketType { get; set; } = string.Empty;

        [JsonPropertyName("ticket_code")]
        public string TicketCode { get; set; } = string.Empty;

        [JsonPropertyName("qr_token")]
        public string QrToken { get; set; } = string.Empty;

        [JsonPropertyName("is_checked_in")]
        public bool IsCheckedIn { get; set; }

        [JsonPropertyName("checked_in_at")]
        public DateTime? CheckedInAt { get; set; }

        [JsonPropertyName("registered_at")]
        public DateTime RegisteredAt { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("payment_status")]
        public string PaymentStatus { get; set; } = "Pending";

        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; set; }
    }

    internal class SupabaseEvent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("event_date")]
        public DateTime? EventDate { get; set; }

        [JsonPropertyName("venue")]
        public string Venue { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("logo_path")]
        public string? LogoPath { get; set; }
    }
}
