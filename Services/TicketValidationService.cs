using Ticket.Models;

namespace Ticket.Services
{
    public enum ValidationResultStatus
    {
        Valid,
        AlreadyUsed,
        Invalid
    }

    public class ValidationResult
    {
        public ValidationResultStatus Status { get; set; }
        public Attendee? Attendee { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface ITicketValidationService
    {
        Task<ValidationResult> ValidateTicketAsync(string ticketId, string token);
    }

    public class TicketValidationService : ITicketValidationService
    {
        private readonly ISupabaseClient _supabase;

        public TicketValidationService(ISupabaseClient supabase)
        {
            _supabase = supabase;
        }

        public async Task<ValidationResult> ValidateTicketAsync(string ticketId, string token)
        {
            var attendee = await _supabase.GetAttendeeByTicketCodeAsync(ticketId);

            if (attendee is null)
            {
                return new ValidationResult
                {
                    Status = ValidationResultStatus.Invalid,
                    Message = "Invalid ticket. No attendee found."
                };
            }

            if (attendee.QrToken != token)
            {
                return new ValidationResult
                {
                    Status = ValidationResultStatus.Invalid,
                    Message = "Invalid ticket token."
                };
            }

            if (attendee.IsCheckedIn)
            {
                return new ValidationResult
                {
                    Status = ValidationResultStatus.AlreadyUsed,
                    Attendee = attendee,
                    Message = attendee.CheckedInAt.HasValue
                        ? $"Already checked in at {attendee.CheckedInAt.Value:HH:mm:ss}"
                        : "Already checked in"
                };
            }

            attendee.IsCheckedIn = true;
            attendee.CheckedInAt = DateTime.UtcNow;
            await _supabase.SaveAttendeeAsync(attendee);

            return new ValidationResult
            {
                Status = ValidationResultStatus.Valid,
                Attendee = attendee,
                Message = "Check-in successful!"
            };
        }
    }
}
