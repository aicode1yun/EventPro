using SQLite;

namespace Ticket.Models
{
    [Table("Events")]
    public class Event
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string EventName { get; set; } = "EventPro Conference";
        public DateTime EventDate { get; set; } = DateTime.Today.AddMonths(1);
        public string Venue { get; set; } = "Main Hall";
        public string? Description { get; set; }
        public string? LogoPath { get; set; }
    }
}
