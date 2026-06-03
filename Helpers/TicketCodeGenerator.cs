namespace Ticket.Helpers
{
    public static class TicketCodeGenerator
    {
        private static readonly Random _random = new();

        public static string GenerateCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new char[6];
            for (int i = 0; i < 6; i++)
                code[i] = chars[_random.Next(chars.Length)];
            return $"TKT-{new string(code)}";
        }

        public static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
