using SQLite;

namespace Ticket.Models
{
    [Table("Users")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Operator;
        public int? EventId { get; set; }  // nullable: admin can manage all events
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
