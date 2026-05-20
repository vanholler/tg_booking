namespace TgBooking.Domain.Entities;

public class BookingDetails
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public long UserTelegramId { get; set; }
    public string UserName { get; set; } = "";
    public string UserPhone { get; set; } = "";
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
    public decimal ServicePrice { get; set; }
    public int ServiceDurationMinutes { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly BookingTime { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
