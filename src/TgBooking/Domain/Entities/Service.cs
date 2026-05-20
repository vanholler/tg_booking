namespace TgBooking.Domain.Entities;

public class Service
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}
