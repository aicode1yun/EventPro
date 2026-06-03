using Microsoft.Maui.Storage;
using SQLite;
using Ticket.Helpers;
using Ticket.Models;

namespace Ticket.Data
{
    public class AppDbContext
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public AppDbContext() : this(Path.Combine(FileSystem.AppDataDirectory, "eventpro.db3"))
        {
        }

        public AppDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        public void SetDatabase(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_database is not null)
                return _database;

            _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Attendee>();
            await _database.CreateTableAsync<Event>();

            // Create index for TicketCode to improve lookups
            try
            {
                await _database.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_TicketCode ON Attendees (TicketCode);");
            }
            catch
            {
                // If index creation fails, continue - may be older DB or platform specific
            }

            await SeedDataAsync();

            return _database;
        }

        private async Task SeedDataAsync()
        {
            var existingUsers = await _database!.Table<User>().CountAsync();
            if (existingUsers == 0)
            {
                var hashed = Helpers.PasswordHasher.HashPassword(Constants.DefaultPassword);
                await _database.InsertAsync(new User
                {
                    Email = Constants.DefaultEmail,
                    PasswordHash = hashed,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var existingEvents = await _database!.Table<Event>().CountAsync();
            if (existingEvents == 0)
            {
                await _database.InsertAsync(new Event
                {
                    EventName = "EventPro Conference",
                    EventDate = DateTime.Today.AddMonths(1),
                    Venue = "Main Hall",
                    Description = "Annual tech conference"
                });
            }
        }

        public Task<SQLiteAsyncConnection> Database => GetConnectionAsync();

        public async Task<List<Attendee>> GetAttendeesAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<Attendee>().OrderByDescending(a => a.RegisteredAt).ToListAsync();
        }

        public async Task<List<Attendee>> SearchAttendeesAsync(string query)
        {
            var db = await GetConnectionAsync();
            var like = $"%{query}%";
            return await db.QueryAsync<Attendee>("SELECT * FROM Attendees WHERE FullName LIKE ? OR TicketCode LIKE ? OR PhoneNumber LIKE ? ORDER BY RegisteredAt DESC", like, like, like);
        }

        public async Task<Attendee?> GetAttendeeByTicketCodeAsync(string ticketCode)
        {
            var db = await GetConnectionAsync();
            return await db.Table<Attendee>().FirstOrDefaultAsync(a => a.TicketCode == ticketCode);
        }

        public async Task<Attendee?> GetAttendeeByIdAsync(int id)
        {
            var db = await GetConnectionAsync();
            return await db.Table<Attendee>().FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Attendee?> GetAttendeeByPhoneAsync(string phone)
        {
            var db = await GetConnectionAsync();
            return await db.Table<Attendee>().FirstOrDefaultAsync(a => a.PhoneNumber == phone);
        }

        public async Task<int> SaveAttendeeAsync(Attendee attendee)
        {
            var db = await GetConnectionAsync();
            if (attendee.Id != 0)
                return await db.UpdateAsync(attendee);

            // Ensure TicketCode uniqueness: retry generation if conflict
            var tries = 0;
            while (string.IsNullOrWhiteSpace(attendee.TicketCode) || (await db.Table<Attendee>().Where(a => a.TicketCode == attendee.TicketCode).FirstOrDefaultAsync()) is not null)
            {
                attendee.TicketCode = TicketCodeGenerator.GenerateCode();
                tries++;
                if (tries > 10) break; // safety
            }

            return await db.InsertAsync(attendee);
        }

        public async Task<int> DeleteAttendeeAsync(Attendee attendee)
        {
            var db = await GetConnectionAsync();
            return await db.DeleteAsync(attendee);
        }

        public async Task<User?> GetUserAsync(string email)
        {
            var db = await GetConnectionAsync();
            return await db.Table<User>().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<int> SaveUserAsync(User user)
        {
            var db = await GetConnectionAsync();
            if (user.Id != 0)
                return await db.UpdateAsync(user);
            return await db.InsertAsync(user);
        }

        public async Task<Event?> GetEventAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<Event>().FirstOrDefaultAsync();
        }

        public async Task<int> SaveEventAsync(Event evt)
        {
            var db = await GetConnectionAsync();
            if (evt.Id != 0)
                return await db.UpdateAsync(evt);
            return await db.InsertAsync(evt);
        }

        public async Task<int> GetTotalAttendeesAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<Attendee>().CountAsync();
        }

        public async Task<int> GetCheckedInCountAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<Attendee>().Where(a => a.IsCheckedIn).CountAsync();
        }

        public async Task SyncAllAttendeesAsync(List<Attendee> attendees)
        {
            var db = await GetConnectionAsync();
            await db.RunInTransactionAsync(conn =>
            {
                foreach (var a in attendees)
                {
                    var exists = conn.FindWithQuery<Attendee>("SELECT * FROM Attendees WHERE TicketCode = ?", a.TicketCode);
                    if (exists is not null)
                    {
                        a.Id = exists.Id;
                        conn.Update(a);
                    }
                    else
                    {
                        a.Id = 0;
                        conn.Insert(a);
                    }
                }
            });
        }
    }
}
