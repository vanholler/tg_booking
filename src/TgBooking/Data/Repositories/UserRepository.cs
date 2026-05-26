using Dapper;
using Npgsql;
using TgBooking.Domain.Entities;

namespace TgBooking.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<User?> GetByTelegramIdAsync(long telegramId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "SELECT id AS Id, telegram_id AS TelegramId, name AS Name, phone AS Phone, created_at AS CreatedAt FROM users WHERE telegram_id = @TelegramId";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { TelegramId = telegramId });
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<User> CreateAsync(long telegramId, string name, string phone)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"INSERT INTO users (telegram_id, name, phone) VALUES (@TelegramId, @Name, @Phone)
                    RETURNING id AS Id, telegram_id AS TelegramId, name AS Name, phone AS Phone, created_at AS CreatedAt";
            return await connection.QuerySingleAsync<User>(sql, new { TelegramId = telegramId, Name = name, Phone = phone });
        }
        catch (Exception)
        {
            throw;
        }
    }
}
