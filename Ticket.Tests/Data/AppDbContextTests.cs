using Ticket.Data;
using Ticket.Helpers;
using Ticket.Models;

namespace Ticket.Tests.Data;

public class AppDbContextTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _dbPath;

    public AppDbContextTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"eventpro_test_{Guid.NewGuid():N}.db3");
        _db = new AppDbContext(_dbPath);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task Database_CreatesTablesAndSeeds()
    {
        var attendees = await _db.GetAttendeesAsync();
        Assert.NotNull(attendees);
    }

    [Fact]
    public async Task SeedData_CreatesDefaultUser()
    {
        var user = await _db.GetUserAsync("admin@eventpro.com");
        Assert.NotNull(user);
        Assert.True(PasswordHasher.Verify(user.PasswordHash, "Admin@123"));
    }

    [Fact]
    public async Task SeedData_CreatesDefaultEvent()
    {
        var evt = await _db.GetEventAsync();
        Assert.NotNull(evt);
        Assert.Equal("EventPro Conference", evt.EventName);
    }

    [Fact]
    public async Task SaveAttendee_InsertsNew()
    {
        var attendee = new Attendee
        {
            FullName = "John Doe",
            PhoneNumber = "+1234567890",
            TicketType = "VIP",
            TicketCode = TicketCodeGenerator.GenerateCode(),
            QrToken = TicketCodeGenerator.GenerateToken(),
            RegisteredAt = DateTime.UtcNow
        };

        var count = await _db.SaveAttendeeAsync(attendee);
        Assert.Equal(1, count);
        Assert.NotEqual(0, attendee.Id);
    }

    [Fact]
    public async Task SaveAttendee_UpdatesExisting()
    {
        var attendee = new Attendee
        {
            FullName = "Jane Doe",
            PhoneNumber = "+0987654321",
            TicketType = "General",
            TicketCode = TicketCodeGenerator.GenerateCode(),
            QrToken = TicketCodeGenerator.GenerateToken(),
            RegisteredAt = DateTime.UtcNow
        };
        await _db.SaveAttendeeAsync(attendee);

        attendee.FullName = "Jane Smith";
        attendee.IsCheckedIn = true;
        var count = await _db.SaveAttendeeAsync(attendee);

        Assert.Equal(1, count);

        var saved = await _db.GetAttendeeByTicketCodeAsync(attendee.TicketCode);
        Assert.NotNull(saved);
        Assert.Equal("Jane Smith", saved.FullName);
        Assert.True(saved.IsCheckedIn);
    }

    [Fact]
    public async Task GetAttendees_ReturnsAllOrderedByDate()
    {
        var a1 = new Attendee { FullName = "A", PhoneNumber = "+1", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow.AddHours(-2) };
        var a2 = new Attendee { FullName = "B", PhoneNumber = "+2", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow.AddHours(-1) };
        var a3 = new Attendee { FullName = "C", PhoneNumber = "+3", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };

        await _db.SaveAttendeeAsync(a1);
        await _db.SaveAttendeeAsync(a2);
        await _db.SaveAttendeeAsync(a3);

        var all = await _db.GetAttendeesAsync();
        var filtered = all.Where(a => a.PhoneNumber is "+1" or "+2" or "+3").ToList();

        Assert.Equal(3, filtered.Count);
        Assert.Equal("C", filtered[0].FullName);
        Assert.Equal("B", filtered[1].FullName);
        Assert.Equal("A", filtered[2].FullName);
    }

    [Fact]
    public async Task SearchAttendees_ByFullName()
    {
        var a = new Attendee { FullName = "Searchable Person", PhoneNumber = "+999", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };
        await _db.SaveAttendeeAsync(a);

        var results = await _db.SearchAttendeesAsync("searchable");
        Assert.Contains(results, r => r.PhoneNumber == "+999");
    }

    [Fact]
    public async Task SearchAttendees_ByTicketCode()
    {
        var code = TicketCodeGenerator.GenerateCode();
        var a = new Attendee { FullName = "Code Search", PhoneNumber = "+888", TicketType = "G", TicketCode = code, QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };
        await _db.SaveAttendeeAsync(a);

        var suffix = code[4..];
        var results = await _db.SearchAttendeesAsync(suffix);
        Assert.Contains(results, r => r.TicketCode == code);
    }

    [Fact]
    public async Task SearchAttendees_ByPhone()
    {
        var a = new Attendee { FullName = "Phone Search", PhoneNumber = "+7777777777", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };
        await _db.SaveAttendeeAsync(a);

        var results = await _db.SearchAttendeesAsync("7777");
        Assert.Contains(results, r => r.PhoneNumber == "+7777777777");
    }

    [Fact]
    public async Task SearchAttendees_EmptyQuery_ReturnsAll()
    {
        var all = await _db.GetAttendeesAsync();
        var results = await _db.SearchAttendeesAsync("");
        Assert.Equal(all.Count, results.Count);
    }

    [Fact]
    public async Task GetAttendeeByTicketCode_FindsExisting()
    {
        var code = TicketCodeGenerator.GenerateCode();
        var a = new Attendee { FullName = "Ticket Lookup", PhoneNumber = "+666", TicketType = "G", TicketCode = code, QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };
        await _db.SaveAttendeeAsync(a);

        var found = await _db.GetAttendeeByTicketCodeAsync(code);
        Assert.NotNull(found);
        Assert.Equal(a.FullName, found.FullName);
    }

    [Fact]
    public async Task GetAttendeeByTicketCode_ReturnsNullForMissing()
    {
        var found = await _db.GetAttendeeByTicketCodeAsync("TKT-NONEXIST");
        Assert.Null(found);
    }

    [Fact]
    public async Task GetAttendeeByPhone_FindsExisting()
    {
        var a = new Attendee { FullName = "Phone Lookup", PhoneNumber = "+5555555555", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };
        await _db.SaveAttendeeAsync(a);

        var found = await _db.GetAttendeeByPhoneAsync("+5555555555");
        Assert.NotNull(found);
        Assert.Equal(a.FullName, found.FullName);
    }

    [Fact]
    public async Task DeleteAttendee_RemovesFromDb()
    {
        var a = new Attendee { FullName = "Delete Me", PhoneNumber = "+444", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };
        await _db.SaveAttendeeAsync(a);

        await _db.DeleteAttendeeAsync(a);

        var found = await _db.GetAttendeeByTicketCodeAsync(a.TicketCode);
        Assert.Null(found);
    }

    [Fact]
    public async Task GetTotalAttendees_ReturnsCorrectCount()
    {
        var before = await _db.GetTotalAttendeesAsync();

        var a = new Attendee { FullName = "Count Test", PhoneNumber = "+333", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };
        await _db.SaveAttendeeAsync(a);

        var after = await _db.GetTotalAttendeesAsync();
        Assert.Equal(before + 1, after);
    }

    [Fact]
    public async Task GetCheckedInCount_ReturnsCorrectCount()
    {
        var a1 = new Attendee { FullName = "C1", PhoneNumber = "+111", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow, IsCheckedIn = true, CheckedInAt = DateTime.UtcNow };
        var a2 = new Attendee { FullName = "C2", PhoneNumber = "+222", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow, IsCheckedIn = true, CheckedInAt = DateTime.UtcNow };
        var a3 = new Attendee { FullName = "Not C", PhoneNumber = "+223", TicketType = "G", TicketCode = TicketCodeGenerator.GenerateCode(), QrToken = TicketCodeGenerator.GenerateToken(), RegisteredAt = DateTime.UtcNow };

        await _db.SaveAttendeeAsync(a1);
        await _db.SaveAttendeeAsync(a2);
        await _db.SaveAttendeeAsync(a3);

        var count = await _db.GetCheckedInCountAsync();
        Assert.True(count >= 2);
    }

    [Fact]
    public async Task SaveEvent_InsertsAndUpdates()
    {
        var evt = await _db.GetEventAsync();
        Assert.NotNull(evt);

        evt.EventName = "Updated Event Name";
        evt.Venue = "New Venue";
        await _db.SaveEventAsync(evt);

        var reloaded = await _db.GetEventAsync();
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Event Name", reloaded.EventName);
        Assert.Equal("New Venue", reloaded.Venue);
    }

    [Fact]
    public async Task GetUser_ReturnsNullForUnknown()
    {
        var user = await _db.GetUserAsync("unknown@test.com");
        Assert.Null(user);
    }
}
