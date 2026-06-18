using Ticket.Helpers;
using Ticket.Models;

namespace Ticket.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string email, string password, bool rememberMe);
        Task LogoutAsync();
        Task<bool> IsLoggedInAsync();
        Task<UserRole?> GetCurrentUserRoleAsync();
        Task SetCurrentUserRoleAsync(UserRole role);
    }

    public class AuthService : IAuthService
    {
        private readonly ISupabaseClient _supabase;
        private const string RoleKey = "user_role";

        public AuthService(ISupabaseClient supabase)
        {
            _supabase = supabase;
        }

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
        {
            UserRole role;

            if (email == Constants.OperatorEmail && password == Constants.OperatorPassword)
                role = UserRole.Operator;
            else if (email == Constants.DefaultEmail && password == Constants.DefaultPassword)
                role = UserRole.Admin;
            else
                return false;

            // Both use the same Supabase Auth session (admin) to get a valid Bearer token
            var result = await _supabase.LoginAsync(Constants.DefaultEmail, Constants.DefaultPassword, rememberMe);
            if (result)
            {
                await SetCurrentUserRoleAsync(role);
            }
            return result;
        }

        public async Task LogoutAsync()
        {
            await _supabase.LogoutAsync();
            try
            {
                SecureStorage.Default.Remove(RoleKey);
            }
            catch { }
        }

        public async Task<bool> IsLoggedInAsync()
        {
            return await _supabase.IsLoggedInAsync();
        }

        public async Task<UserRole?> GetCurrentUserRoleAsync()
        {
            try
            {
                var roleStr = await SecureStorage.Default.GetAsync(RoleKey);
                if (string.IsNullOrEmpty(roleStr)) return null;
                if (int.TryParse(roleStr, out var roleInt) && Enum.IsDefined(typeof(UserRole), roleInt))
                {
                    return (UserRole)roleInt;
                }
            }
            catch { }
            return null;
        }

        public async Task SetCurrentUserRoleAsync(UserRole role)
        {
            try
            {
                await SecureStorage.Default.SetAsync(RoleKey, ((int)role).ToString());
            }
            catch { }
        }
    }
}
