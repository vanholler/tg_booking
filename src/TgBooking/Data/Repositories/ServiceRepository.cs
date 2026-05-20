using Dapper;
using Npgsql;
using TgBooking.Domain.Entities;

namespace TgBooking.Data.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly string _connectionString;

    public ServiceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<Service>> GetAllAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = "SELECT id AS Id, name AS Name, price AS Price, duration_minutes AS DurationMinutes FROM services ORDER BY name";
        var list = await connection.QueryAsync<Service>(sql);
        return list.ToList();
    }

    public async Task<Service?> GetByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = "SELECT id AS Id, name AS Name, price AS Price, duration_minutes AS DurationMinutes FROM services WHERE id = @Id";
        return await connection.QuerySingleOrDefaultAsync<Service>(sql, new { Id = id });
    }

    public async Task<Service> CreateAsync(string name, decimal price, int durationMinutes)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"INSERT INTO services (name, price, duration_minutes) VALUES (@Name, @Price, @DurationMinutes)
                    RETURNING id AS Id, name AS Name, price AS Price, duration_minutes AS DurationMinutes";
        return await connection.QuerySingleAsync<Service>(sql, new { Name = name, Price = price, DurationMinutes = durationMinutes });
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync("DELETE FROM services WHERE id = @Id", new { Id = id });
        return affected > 0;
    }
}
