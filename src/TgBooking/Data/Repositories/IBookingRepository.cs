using TgBooking.Domain.Entities;

namespace TgBooking.Data.Repositories;

public interface IBookingRepository
{
    Task<Booking> CreateAsync(int userId, int serviceId, DateOnly date, TimeOnly time, string status);
    Task<BookingDetails?> GetDetailsByIdAsync(int id);
    Task<List<BookingDetails>> GetAllDetailsAsync();
    Task<List<TimeOnly>> GetOccupiedTimesAsync(int serviceId, DateOnly date);
    Task<bool> UpdateStatusAsync(int id, string newStatus, string oldStatus);
}
