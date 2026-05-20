using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Telegram.Bot;
using TgBooking.Bot;
using TgBooking.Configuration;
using TgBooking.Data.Repositories;
using TgBooking.Services;

static string ReadBotToken(IConfiguration configuration)
{
    var token = (configuration["BOT_TOKEN"] ?? configuration["BotToken"] ?? "").Trim();

    if (token.Length >= 2 && token.StartsWith('"') && token.EndsWith('"'))
        token = token[1..^1].Trim();

    return token;
}

static long ReadAdminId(IConfiguration configuration)
{
    var raw = configuration["ADMIN_TELEGRAM_ID"] ?? configuration["AdminTelegramId"] ?? "0";
    long.TryParse(raw.Trim(), out var adminId);
    return adminId;
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        var settings = new BotSettings
        {
            BotToken = ReadBotToken(configuration),
            AdminTelegramId = ReadAdminId(configuration)
        };

        if (string.IsNullOrWhiteSpace(settings.BotToken) ||
            settings.BotToken == "telegram_bot_token" ||
            !settings.BotToken.Contains(':'))
        {
            throw new InvalidOperationException(
                "BOT_TOKEN не задан или некорректен. Укажите токен от @BotFather в файле .env в корне проекта.");
        }

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? configuration["ConnectionStrings__Postgres"]
            ?? "";

        var botToken = settings.BotToken;

        services.AddSingleton(settings);
        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botToken));

        services.AddSingleton<IUserRepository>(_ => new UserRepository(connectionString));
        services.AddSingleton<IServiceRepository>(_ => new ServiceRepository(connectionString));
        services.AddSingleton<IBookingRepository>(_ => new BookingRepository(connectionString));

        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<UserStateStore>();
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
