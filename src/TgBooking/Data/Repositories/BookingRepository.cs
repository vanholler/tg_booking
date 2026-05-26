using Dapper;
using Npgsql;
using TgBooking.Domain.Entities;

namespace TgBooking.Data.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly string _connectionString;

    public BookingRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Booking> CreateAsync(int userId, int serviceId, DateOnly date, TimeOnly time, string status)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"INSERT INTO bookings (user_id, service_id, booking_date, booking_time, status)
                    VALUES (@UserId, @ServiceId, @BookingDate, @BookingTime, @Status)
                    RETURNING id AS Id, user_id AS UserId, service_id AS ServiceId, booking_date AS BookingDate,
                              booking_time AS BookingTime, status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt";
            var row = await connection.QuerySingleAsync<BookingRow>(sql, new
            {
                UserId = userId,
                ServiceId = serviceId,
                BookingDate = date.ToDateTime(TimeOnly.MinValue),
                BookingTime = time.ToTimeSpan(),
                Status = status
            });
            return ToBooking(row);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BookingDetails?> GetDetailsByIdAsync(int id)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"SELECT b.id AS Id, b.user_id AS UserId, u.telegram_id AS UserTelegramId, u.name AS UserName, u.phone AS UserPhone,
                    b.service_id AS ServiceId, s.name AS ServiceName, s.price AS ServicePrice, s.duration_minutes AS ServiceDurationMinutes,
                    b.booking_date AS BookingDate, b.booking_time AS BookingTime, b.status AS Status, b.created_at AS CreatedAt
                    FROM bookings b
                    INNER JOIN users u ON u.id = b.user_id
                    INNER JOIN services s ON s.id = b.service_id
                    WHERE b.id = @Id";
            var row = await connection.QuerySingleOrDefaultAsync<BookingDetailsRow>(sql, new { Id = id });
            return row == null ? null : ToBookingDetails(row);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<BookingDetails>> GetAllDetailsAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"SELECT b.id AS Id, b.user_id AS UserId, u.telegram_id AS UserTelegramId, u.name AS UserName, u.phone AS UserPhone,
                    b.service_id AS ServiceId, s.name AS ServiceName, s.price AS ServicePrice, s.duration_minutes AS ServiceDurationMinutes,
                    b.booking_date AS BookingDate, b.booking_time AS BookingTime, b.status AS Status, b.created_at AS CreatedAt
                    FROM bookings b
                    INNER JOIN users u ON u.id = b.user_id
                    INNER JOIN services s ON s.id = b.service_id
                    ORDER BY b.created_at DESC";
            var list = await connection.QueryAsync<BookingDetailsRow>(sql);
            return list.Select(ToBookingDetails).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<TimeOnly>> GetOccupiedTimesAsync(int serviceId, DateOnly date)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"SELECT booking_time AS BookingTime FROM bookings
                    WHERE service_id = @ServiceId AND booking_date = @BookingDate AND status IN ('Pending', 'Confirmed')";
            var list = await connection.QueryAsync<TimeOnly>(sql, new
            {
                ServiceId = serviceId,
                BookingDate = date.ToDateTime(TimeOnly.MinValue)
            });
            return list.ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, string newStatus, string oldStatus)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "UPDATE bookings SET status = @NewStatus, updated_at = NOW() WHERE id = @Id AND status = @OldStatus";
            var affected = await connection.ExecuteAsync(sql, new { Id = id, NewStatus = newStatus, OldStatus = oldStatus });
            return affected > 0;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static Booking ToBooking(BookingRow row) => new()
    {
        Id = row.Id,
        UserId = row.UserId,
        ServiceId = row.ServiceId,
        BookingDate = row.BookingDate,
        BookingTime = row.BookingTime,
        Status = row.Status,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };

    private static BookingDetails ToBookingDetails(BookingDetailsRow row) => new()
    {
        Id = row.Id,
        UserId = row.UserId,
        UserTelegramId = row.UserTelegramId,
        UserName = row.UserName,
        UserPhone = row.UserPhone,
        ServiceId = row.ServiceId,
        ServiceName = row.ServiceName,
        ServicePrice = row.ServicePrice,
        ServiceDurationMinutes = row.ServiceDurationMinutes,
        BookingDate = row.BookingDate,
        BookingTime = row.BookingTime,
        Status = row.Status,
        CreatedAt = row.CreatedAt
    };

    private class BookingRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ServiceId { get; set; }
        public DateOnly BookingDate { get; set; }
        public TimeOnly BookingTime { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class BookingDetailsRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public long UserTelegramId { get; set; }
        public string UserName { get; set; } = "";
        public string UserPhone { get; set; } = "";
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = "";
        public decimal ServicePrice { get; set; }
        public int ServiceDurationMinutes { get; set; }
        public DateOnly BookingDate { get; set; }
        public TimeOnly BookingTime { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
