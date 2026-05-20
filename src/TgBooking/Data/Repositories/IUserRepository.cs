using TgBooking.Domain.Entities;

namespace TgBooking.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByTelegramIdAsync(long telegramId);
    Task<User> CreateAsync(long telegramId, string name, string phone);
}
