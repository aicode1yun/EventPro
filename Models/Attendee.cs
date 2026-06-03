using SQLite;

namespace Ticket.Models
{
    [Table("Attendees")]
    public class Attendee
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        [Indexed]
        public string PhoneNumber { get; set; } = string.Empty;

        public string TicketType { get; set; } = string.Empty;

        [Indexed(Name = "IX_TicketCode", Order = 1)]
        public string TicketCode { get; set; } = string.Empty;

        public string QrToken { get; set; } = string.Empty;
        public bool IsCheckedIn { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public string? PhotoUrl { get; set; }
    }
}
