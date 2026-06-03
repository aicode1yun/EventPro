using System.Text;
using Ticket.Models;
using Ticket.Services;
using Ticket.Tests;

namespace Ticket.Tests.Services;

public class QrCodeServiceTests
{
    private readonly QrCodeService _service = new();

    private static Attendee CreateTestAttendee()
    {
        return new Attendee
        {
            Id = 1,
            FullName = "John Doe",
            TicketCode = "TKT-ABC123",
            QrToken = "abcdef1234567890abcdef1234567890",
            TicketType = "VIP"
        };
    }

    [Fact]
    public void GenerateQrCode_ReturnsNonEmptyBytes()
    {
        var attendee = CreateTestAttendee();
        var bytes = _service.GenerateQrCode(attendee);
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateQrCode_ReturnsPngBytes()
    {
        var attendee = CreateTestAttendee();
        var bytes = _service.GenerateQrCode(attendee);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal("PNG", Encoding.ASCII.GetString(bytes, 1, 3));
    }

    [Fact]
    public void GenerateQrCode_ProducesDifferentBytesForDifferentAttendees()
    {
        var a1 = CreateTestAttendee();
        var a2 = CreateTestAttendee();
        a2.TicketCode = "TKT-XYZ789";
        a2.QrToken = "differenttoken1234567890abcdef";

        var b1 = _service.GenerateQrCode(a1);
        var b2 = _service.GenerateQrCode(a2);

        Assert.NotEqual(b1, b2);
    }

    [Fact]
    public void ParseQrPayload_ReturnsNullForInvalidJson()
    {
        var result = _service.ParseQrPayload("not-json");
        Assert.Null(result);
    }

    [Fact]
    public void ParseQrPayload_ReturnsNullForEmptyString()
    {
        var result = _service.ParseQrPayload(string.Empty);
        Assert.Null(result);
    }

    [Fact]
    public void ParseQrPayload_ParsesValidPayload()
    {
        var json = """{"TicketId":"TKT-ABC123","Token":"mytoken123"}""";
        var result = _service.ParseQrPayload(json);
        Assert.NotNull(result);
        Assert.Equal("TKT-ABC123", result.TicketId);
        Assert.Equal("mytoken123", result.Token);
    }

    [Fact]
    public void ParseQrPayload_HandlesMissingFields()
    {
        var json = """{"TicketId":"TKT-ABC123"}""";
        var result = _service.ParseQrPayload(json);
        Assert.NotNull(result);
        Assert.Equal("TKT-ABC123", result.TicketId);
        Assert.Equal(string.Empty, result.Token);
    }

    [Fact]
    public void Roundtrip_GenerateThenParse()
    {
        var attendee = CreateTestAttendee();
        var bytes = _service.GenerateQrCode(attendee);
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }
}
