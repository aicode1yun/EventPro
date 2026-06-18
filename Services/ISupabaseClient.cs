using Ticket.Models;

namespace Ticket.Services
{
    public interface ISupabaseClient
    {
        // Auth
        Task<bool> LoginAsync(string email, string password, bool rememberMe);
        Task LogoutAsync();
        Task<bool> IsLoggedInAsync();

        // Attendees
        Task<List<Attendee>> GetAttendeesAsync();
        Task<List<Attendee>> SearchAttendeesAsync(string query);
        Task<Attendee?> GetAttendeeByTicketCodeAsync(string ticketCode);
        Task<Attendee?> GetAttendeeByIdAsync(int id);
        Task<Attendee?> GetAttendeeByPhoneAsync(string phone);
        Task SaveAttendeeAsync(Attendee attendee);
        Task DeleteAttendeeAsync(Attendee attendee);

        // Photo
        Task<string?> UploadPhotoAsync(Stream photoStream, string fileName);
        Task<bool> DeletePhotoAsync(string photoUrl);

        // Event
        Task<Event?> GetEventAsync();
        Task SaveEventAsync(Event evt);

        // Stats
        Task<int> GetTotalAttendeesAsync();
        Task<int> GetCheckedInCountAsync();
        Task<(int total, int checkedIn)> GetDashboardStatsAsync();
    }
}
