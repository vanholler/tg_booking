using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Telegram.Bot;
using TgBooking.Bot;
using TgBooking.Configuration;
using TgBooking.Data.Repositories;
using TgBooking.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        var settings = new BotSettings();
        settings.BotToken = configuration["BotToken"] ?? configuration["BOT_TOKEN"] ?? "";
        if (long.TryParse(configuration["AdminTelegramId"] ?? configuration["ADMIN_TELEGRAM_ID"], out var adminId))
            settings.AdminTelegramId = adminId;

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? configuration["ConnectionStrings__Postgres"]
            ?? "";

        services.AddSingleton(settings);
        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(settings.BotToken));

        services.AddSingleton<IUserRepository>(_ => new UserRepository(connectionString));
        services.AddSingleton<IServiceRepository>(_ => new ServiceRepository(connectionString));
        services.AddSingleton<IBookingRepository>(_ => new BookingRepository(connectionString));

        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<TelegramBotHandler>();
        services.AddHostedService<BookingBotService>();
    })
    .Build();

var config = host.Services.GetRequiredService<IConfiguration>();
var conn = config.GetConnectionString("Postgres") ?? config["ConnectionStrings__Postgres"] ?? "";

for (var i = 1; i <= 10; i++)
{
    try
    {
        await using var db = new NpgsqlConnection(conn);
        await db.OpenAsync();
        Console.WriteLine("База данных подключена");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Подключение " + i + ": " + ex.Message);
        if (i == 10)
            throw;

        await Task.Delay(3000);
    }
}

await host.RunAsync();
