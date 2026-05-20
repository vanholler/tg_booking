namespace TgBooking.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
