using Ticket.Helpers;

namespace Ticket.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string email, string password, bool rememberMe);
        Task LogoutAsync();
        Task<bool> IsLoggedInAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly ISupabaseClient _supabase;

        public AuthService(ISupabaseClient supabase)
        {
            _supabase = supabase;
        }

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
        {
            return await _supabase.LoginAsync(email, password, rememberMe);
        }

        public async Task LogoutAsync()
        {
            await _supabase.LogoutAsync();
        }

        public async Task<bool> IsLoggedInAsync()
        {
            return await _supabase.IsLoggedInAsync();
        }
    }
}
