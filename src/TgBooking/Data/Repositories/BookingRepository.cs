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
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"INSERT INTO bookings (user_id, service_id, booking_date, booking_time, status)
                    VALUES (@UserId, @ServiceId, @BookingDate, @BookingTime, @Status)
                    RETURNING id AS Id, user_id AS UserId, service_id AS ServiceId, booking_date AS BookingDate,
                              booking_time AS BookingTime, status AS Status, created_at AS CreatedAt, updated_at AS UpdatedAt";
        return await connection.QuerySingleAsync<Booking>(sql, new
        {
            UserId = userId,
            ServiceId = serviceId,
            BookingDate = date,
            BookingTime = time,
            Status = status
        });
    }

    public async Task<BookingDetails?> GetDetailsByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"SELECT b.id AS Id, b.user_id AS UserId, u.telegram_id AS UserTelegramId, u.name AS UserName, u.phone AS UserPhone,
                    b.service_id AS ServiceId, s.name AS ServiceName, s.price AS ServicePrice, s.duration_minutes AS ServiceDurationMinutes,
                    b.booking_date AS BookingDate, b.booking_time AS BookingTime, b.status AS Status, b.created_at AS CreatedAt
                    FROM bookings b
                    INNER JOIN users u ON u.id = b.user_id
                    INNER JOIN services s ON s.id = b.service_id
                    WHERE b.id = @Id";
        return await connection.QuerySingleOrDefaultAsync<BookingDetails>(sql, new { Id = id });
    }

    public async Task<List<BookingDetails>> GetAllDetailsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"SELECT b.id AS Id, b.user_id AS UserId, u.telegram_id AS UserTelegramId, u.name AS UserName, u.phone AS UserPhone,
                    b.service_id AS ServiceId, s.name AS ServiceName, s.price AS ServicePrice, s.duration_minutes AS ServiceDurationMinutes,
                    b.booking_date AS BookingDate, b.booking_time AS BookingTime, b.status AS Status, b.created_at AS CreatedAt
                    FROM bookings b
                    INNER JOIN users u ON u.id = b.user_id
                    INNER JOIN services s ON s.id = b.service_id
                    ORDER BY b.created_at DESC";
        var list = await connection.QueryAsync<BookingDetails>(sql);
        return list.ToList();
    }

    public async Task<List<TimeOnly>> GetOccupiedTimesAsync(int serviceId, DateOnly date)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"SELECT booking_time AS BookingTime FROM bookings
                    WHERE service_id = @ServiceId AND booking_date = @BookingDate AND status IN ('Pending', 'Confirmed')";
        var list = await connection.QueryAsync<TimeOnly>(sql, new { ServiceId = serviceId, BookingDate = date });
        return list.ToList();
    }

    public async Task<bool> UpdateStatusAsync(int id, string newStatus, string oldStatus)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = "UPDATE bookings SET status = @NewStatus, updated_at = NOW() WHERE id = @Id AND status = @OldStatus";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, NewStatus = newStatus, OldStatus = oldStatus });
        return affected > 0;
    }
}
