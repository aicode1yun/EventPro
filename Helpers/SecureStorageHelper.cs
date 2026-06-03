namespace Ticket.Helpers
{
    public static class SecureStorageHelper
    {
        private const string IsLoggedInKey = "is_logged_in";
        private const string RememberMeKey = "remember_me";
        private const string ThemeKey = "theme_mode";
        private const string SupabaseAccessTokenKey = "supabase_access_token";
        private const string SupabaseRefreshTokenKey = "supabase_refresh_token";

        public static async Task SetLoggedInAsync(bool value)
        {
            await SecureStorage.Default.SetAsync(IsLoggedInKey, value.ToString());
        }

        public static async Task<bool> IsLoggedInAsync()
        {
            var val = await SecureStorage.Default.GetAsync(IsLoggedInKey);
            return bool.TryParse(val, out var result) && result;
        }

        public static async Task SetRememberMeAsync(bool value)
        {
            await SecureStorage.Default.SetAsync(RememberMeKey, value.ToString());
        }

        public static async Task<bool> GetRememberMeAsync()
        {
            var val = await SecureStorage.Default.GetAsync(RememberMeKey);
            return bool.TryParse(val, out var result) && result;
        }

        public static void SetThemeMode(string mode)
        {
            Preferences.Default.Set(ThemeKey, mode);
        }

        public static string GetThemeMode()
        {
            return Preferences.Default.Get(ThemeKey, "light");
        }

        public static async Task SetSupabaseSessionAsync(string accessToken, string refreshToken)
        {
            await SecureStorage.Default.SetAsync(SupabaseAccessTokenKey, accessToken);
            await SecureStorage.Default.SetAsync(SupabaseRefreshTokenKey, refreshToken);
        }

        public static async Task<string?> GetAccessTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(SupabaseAccessTokenKey);
        }

        public static async Task ClearAuthAsync()
        {
            SecureStorage.Default.Remove(IsLoggedInKey);
            SecureStorage.Default.Remove(RememberMeKey);
            SecureStorage.Default.Remove(SupabaseAccessTokenKey);
            SecureStorage.Default.Remove(SupabaseRefreshTokenKey);
            await Task.CompletedTask;
        }

        public static async Task ClearAsync()
        {
            await ClearAuthAsync();
        }
    }
}
