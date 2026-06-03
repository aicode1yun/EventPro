using Moq;
using Ticket.Helpers;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.Tests.Services;

public class TicketValidationServiceTests
{
    private readonly Mock<ISupabaseClient> _mockClient;
    private readonly Ticket.Services.TicketValidationService _service;

    public TicketValidationServiceTests()
    {
        _mockClient = new Mock<ISupabaseClient>();
        _service = new Ticket.Services.TicketValidationService(_mockClient.Object);
    }

    private static Attendee CreateAttendee(bool isCheckedIn = false)
    {
        return new Attendee
        {
            Id = 1,
            FullName = "Test User",
            PhoneNumber = "+1234567890",
            TicketType = "VIP",
            PaymentStatus = "Paid",
            TicketCode = TicketCodeGenerator.GenerateCode(),
            QrToken = TicketCodeGenerator.GenerateToken(),
            RegisteredAt = DateTime.UtcNow,
            IsCheckedIn = isCheckedIn,
            CheckedInAt = isCheckedIn ? DateTime.UtcNow : null
        };
    }

    [Fact]
    public async Task ValidateTicket_ValidTicket_ReturnsValid()
    {
        var attendee = CreateAttendee();
        _mockClient.Setup(x => x.GetAttendeeByTicketCodeAsync(attendee.TicketCode))
            .ReturnsAsync(attendee);

        var result = await _service.ValidateTicketAsync(attendee.TicketCode, attendee.QrToken);

        Assert.Equal(ValidationResultStatus.Valid, result.Status);
        Assert.NotNull(result.Attendee);
        Assert.Equal(attendee.FullName, result.Attendee.FullName);
        Assert.Contains("successful", result.Message.ToLower());
    }

    [Fact]
    public async Task ValidateTicket_ValidTicket_SetsCheckedIn()
    {
        var attendee = CreateAttendee();
        _mockClient.Setup(x => x.GetAttendeeByTicketCodeAsync(attendee.TicketCode))
            .ReturnsAsync(attendee);

        await _service.ValidateTicketAsync(attendee.TicketCode, attendee.QrToken);

        _mockClient.Verify(x => x.SaveAttendeeAsync(It.Is<Attendee>(a =>
            a.TicketCode == attendee.TicketCode && a.IsCheckedIn)), Times.Once);
    }

    [Fact]
    public async Task ValidateTicket_AlreadyCheckedIn_ReturnsAlreadyUsed()
    {
        var attendee = CreateAttendee(isCheckedIn: true);
        _mockClient.Setup(x => x.GetAttendeeByTicketCodeAsync(attendee.TicketCode))
            .ReturnsAsync(attendee);

        var result = await _service.ValidateTicketAsync(attendee.TicketCode, attendee.QrToken);

        Assert.Equal(ValidationResultStatus.AlreadyUsed, result.Status);
        Assert.NotNull(result.Attendee);
    }

    [Fact]
    public async Task ValidateTicket_WrongToken_ReturnsInvalid()
    {
        var attendee = CreateAttendee();
        _mockClient.Setup(x => x.GetAttendeeByTicketCodeAsync(attendee.TicketCode))
            .ReturnsAsync(attendee);

        var result = await _service.ValidateTicketAsync(attendee.TicketCode, "wrongtoken");

        Assert.Equal(ValidationResultStatus.Invalid, result.Status);
        Assert.Contains("token", result.Message.ToLower());
    }

    [Fact]
    public async Task ValidateTicket_NonExistentTicket_ReturnsInvalid()
    {
        _mockClient.Setup(x => x.GetAttendeeByTicketCodeAsync("TKT-NONEXIST"))
            .ReturnsAsync((Attendee?)null);

        var result = await _service.ValidateTicketAsync("TKT-NONEXIST", "sometoken");

        Assert.Equal(ValidationResultStatus.Invalid, result.Status);
        Assert.Contains("no attendee", result.Message.ToLower());
    }

    [Fact]
    public async Task ValidateTicket_EmptyTicketId_ReturnsInvalid()
    {
        var attendee = CreateAttendee();
        _mockClient.Setup(x => x.GetAttendeeByTicketCodeAsync(string.Empty))
            .ReturnsAsync((Attendee?)null);

        var result = await _service.ValidateTicketAsync(string.Empty, attendee.QrToken);
        Assert.Equal(ValidationResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task ValidateTicket_MultipleScans_OnlyFirstSucceeds()
    {
        var attendee = CreateAttendee();
        _mockClient.SetupSequence(x => x.GetAttendeeByTicketCodeAsync(attendee.TicketCode))
            .ReturnsAsync(attendee)
            .ReturnsAsync(() =>
            {
                attendee.IsCheckedIn = true;
                attendee.CheckedInAt = DateTime.UtcNow;
                return attendee;
            })
            .ReturnsAsync(() => attendee);

        var r1 = await _service.ValidateTicketAsync(attendee.TicketCode, attendee.QrToken);
        Assert.Equal(ValidationResultStatus.Valid, r1.Status);

        var r2 = await _service.ValidateTicketAsync(attendee.TicketCode, attendee.QrToken);
        Assert.Equal(ValidationResultStatus.AlreadyUsed, r2.Status);

        var r3 = await _service.ValidateTicketAsync(attendee.TicketCode, attendee.QrToken);
        Assert.Equal(ValidationResultStatus.AlreadyUsed, r3.Status);
    }
}
