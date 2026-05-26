using TgBooking.Domain.Enums;

namespace TgBooking.Common;

internal static class Guard
{
    public static void NotNull<T>(T? value, string paramName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
    }

    public static void NotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Значение не может быть пустым.", paramName);
    }

    public static void Positive(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Значение должно быть больше нуля.");
    }

    public static void Positive(long value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Значение должно быть больше нуля.");
    }

    public static void NonNegative(decimal value, string paramName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Значение не может быть отрицательным.");
    }

    public static void BookingStatus(string? status, string paramName)
    {
        NotNullOrWhiteSpace(status, paramName);
        if (!Enum.TryParse<BookingStatus>(status, ignoreCase: false, out _))
            throw new ArgumentException("Недопустимый статус бронирования.", paramName);
    }

    public static void BookingTime(TimeOnly time, string paramName = "time")
    {
        if (time.Hour is < 9 or > 20 || time.Minute != 0 || time.Second != 0)
            throw new ArgumentOutOfRangeException(paramName, time, "Время записи должно быть с 09:00 до 20:00.");
    }

    public static void BookingDateNotInPast(DateOnly date, string paramName = "date")
    {
        if (date < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentOutOfRangeException(paramName, date, "Дата записи не может быть в прошлом.");
    }
}
