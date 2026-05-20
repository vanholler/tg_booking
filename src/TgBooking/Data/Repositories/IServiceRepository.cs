using TgBooking.Domain.Entities;

namespace TgBooking.Data.Repositories;

public interface IServiceRepository
{
    Task<List<Service>> GetAllAsync();
    Task<Service?> GetByIdAsync(int id);
    Task<Service> CreateAsync(string name, decimal price, int durationMinutes);
    Task<bool> DeleteAsync(int id);
}
