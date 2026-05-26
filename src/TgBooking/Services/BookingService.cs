using TgBooking.Common;
using TgBooking.Data.Repositories;
using TgBooking.Domain.Entities;
using TgBooking.Domain.Enums;

namespace TgBooking.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        Guard.NotNull(bookingRepository, nameof(bookingRepository));
        _bookingRepository = bookingRepository;
    }

    public List<DateOnly> GetDaysForCurrentMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var count = DateTime.DaysInMonth(today.Year, today.Month);
        var result = new List<DateOnly>();

        for (var day = 1; day <= count; day++)
        {
            var date = new DateOnly(today.Year, today.Month, day);
            if (date >= today)
                result.Add(date);
        }

        return result;
    }

    public List<TimeOnly> GetFreeTimes(List<TimeOnly> busyTimes)
    {
        Guard.NotNull(busyTimes, nameof(busyTimes));

        var busy = busyTimes.ToList();
        var result = new List<TimeOnly>();

        for (var hour = 9; hour <= 20; hour++)
        {
            var slot = new TimeOnly(hour, 0);
            if (!busy.Contains(slot))
                result.Add(slot);
        }

        return result;
    }

    public async Task<BookingDetails> CreateBookingAsync(int userId, int serviceId, DateOnly date, TimeOnly time)
    {
        Guard.Positive(userId, nameof(userId));
        Guard.Positive(serviceId, nameof(serviceId));
        Guard.BookingDateNotInPast(date);
        Guard.BookingTime(time);

        var busy = await _bookingRepository.GetOccupiedTimesAsync(serviceId, date);
        var slotKey = time.ToString("HH:mm");
        if (busy.Any(b => b.ToString("HH:mm") == slotKey))
            throw new Exception("Слот занят");

        var booking = await _bookingRepository.CreateAsync(userId, serviceId, date, time, BookingStatus.Pending.ToString());
        var details = await _bookingRepository.GetDetailsByIdAsync(booking.Id);

        if (details == null)
            throw new Exception("Ошибка сохранения");

        return details;
    }

    public async Task<List<BookingDetails>> GetAllBookingsSortedAsync()
    {
        var list = await _bookingRepository.GetAllDetailsAsync();
        return list
            .OrderBy(x => x.Status != BookingStatus.Pending.ToString())
            .ThenByDescending(x => x.CreatedAt)
            .ToList();
    }

    public Task<BookingDetails?> GetByIdAsync(int id)
    {
        Guard.Positive(id, nameof(id));
        return _bookingRepository.GetDetailsByIdAsync(id);
    }

    public async Task<bool> ChangeStatusAsync(int bookingId, BookingStatus newStatus)
    {
        Guard.Positive(bookingId, nameof(bookingId));
        if (!Enum.IsDefined(newStatus))
            throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, "Недопустимый статус бронирования.");

        var booking = await _bookingRepository.GetDetailsByIdAsync(bookingId);
        if (booking == null)
            return false;

        if (booking.Status != BookingStatus.Pending.ToString())
            return false;

        return await _bookingRepository.UpdateStatusAsync(
            bookingId,
            newStatus.ToString(),
            BookingStatus.Pending.ToString());
    }

    public Task<List<TimeOnly>> GetBusyTimesAsync(int serviceId, DateOnly date)
    {
        Guard.Positive(serviceId, nameof(serviceId));
        return _bookingRepository.GetOccupiedTimesAsync(serviceId, date);
    }
}
