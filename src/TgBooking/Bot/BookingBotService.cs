using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgBooking.Common;

namespace TgBooking.Bot;

public class BookingBotService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly TelegramBotHandler _handler;

    public BookingBotService(ITelegramBotClient bot, TelegramBotHandler handler)
    {
        Guard.NotNull(bot, nameof(bot));
        Guard.NotNull(handler, nameof(handler));
        _bot = bot;
        _handler = handler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        _bot.StartReceiving(
            async (client, update, token) => await _handler.OnUpdate(update, token),
            (client, ex, token) =>
            {
                Console.WriteLine("Ошибка: " + ex.Message);
                return Task.CompletedTask;
            },
            options,
            stoppingToken);

        var me = await _bot.GetMe(stoppingToken);
        Console.WriteLine("Бот работает: @" + me.Username);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
