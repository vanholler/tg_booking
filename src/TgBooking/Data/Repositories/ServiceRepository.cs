using Dapper;
using Npgsql;
using TgBooking.Common;
using TgBooking.Domain.Entities;

namespace TgBooking.Data.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly string _connectionString;

    public ServiceRepository(string connectionString)
    {
        Guard.NotNullOrWhiteSpace(connectionString, nameof(connectionString));
        _connectionString = connectionString;
    }

    public async Task<List<Service>> GetAllAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "SELECT id AS Id, name AS Name, price AS Price, duration_minutes AS DurationMinutes FROM services ORDER BY name";
            var list = await connection.QueryAsync<Service>(sql);
            return list.ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Service?> GetByIdAsync(int id)
    {
        Guard.Positive(id, nameof(id));

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "SELECT id AS Id, name AS Name, price AS Price, duration_minutes AS DurationMinutes FROM services WHERE id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Service>(sql, new { Id = id });
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Service> CreateAsync(string name, decimal price, int durationMinutes)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.NonNegative(price, nameof(price));
        Guard.Positive(durationMinutes, nameof(durationMinutes));

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"INSERT INTO services (name, price, duration_minutes) VALUES (@Name, @Price, @DurationMinutes)
                    RETURNING id AS Id, name AS Name, price AS Price, duration_minutes AS DurationMinutes";
            return await connection.QuerySingleAsync<Service>(sql, new { Name = name, Price = price, DurationMinutes = durationMinutes });
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        Guard.Positive(id, nameof(id));

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            const string sql = "DELETE FROM services WHERE id = @Id";
            var affected = await connection.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
