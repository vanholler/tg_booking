using TgBooking.Domain.Entities;
using TgBooking.Domain.Enums;

namespace TgBooking.Services;

public interface IBookingService
{
    List<DateOnly> GetDaysForCurrentMonth();
    List<TimeOnly> GetFreeTimes(List<TimeOnly> busyTimes);
    Task<BookingDetails> CreateBookingAsync(int userId, int serviceId, DateOnly date, TimeOnly time);
    Task<List<BookingDetails>> GetAllBookingsSortedAsync();
    Task<BookingDetails?> GetByIdAsync(int id);
    Task<bool> ChangeStatusAsync(int bookingId, BookingStatus newStatus);
    Task<List<TimeOnly>> GetBusyTimesAsync(int serviceId, DateOnly date);
}
