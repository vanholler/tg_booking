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
        settings.BotToken = GetNonEmpty(configuration, "BOT_TOKEN", "BotToken");
        if (long.TryParse(GetNonEmpty(configuration, "ADMIN_TELEGRAM_ID", "AdminTelegramId"), out var adminId) && adminId != 0)
            settings.AdminTelegramId = adminId;

        var connectionString = GetPostgresConnectionString(configuration);

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
var conn = GetPostgresConnectionString(config);

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

static string GetPostgresConnectionString(IConfiguration configuration) =>
    GetNonEmpty(configuration, "ConnectionStrings:Postgres", "ConnectionStrings__Postgres")
    ?? configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Строка подключения Postgres не задана. Укажите ConnectionStrings:Postgres в appsettings.json "
        + "или переменную окружения ConnectionStrings__Postgres.");

static string? GetNonEmpty(IConfiguration configuration, params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
            return value;
    }

    return null;
}
