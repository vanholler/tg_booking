using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgBooking.Configuration;
using TgBooking.Data.Repositories;
using TgBooking.Domain.Enums;
using TgBooking.Services;

namespace TgBooking.Bot;

public class TelegramBotHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly BotSettings _settings;
    private readonly IUserRepository _userRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IBookingService _bookingService;
    private readonly UserStateStore _states = new();

    public TelegramBotHandler(
        ITelegramBotClient bot,
        BotSettings settings,
        IUserRepository userRepository,
        IServiceRepository serviceRepository,
        IBookingService bookingService)
    {
        _bot = bot;
        _settings = settings;
        _userRepository = userRepository;
        _serviceRepository = serviceRepository;
        _bookingService = bookingService;
    }

    public async Task OnUpdate(Update update, CancellationToken ct)
    {
        if (update.CallbackQuery != null)
        {
            await OnCallback(update.CallbackQuery, ct);
            return;
        }

        if (update.Message != null && update.Message.Text != null)
            await OnMessage(update.Message, ct);
    }

    private async Task OnMessage(Message message, CancellationToken ct)
    {
        if (message.From == null || message.Text == null)
            return;

        var chatId = message.Chat.Id;
        var tgId = message.From.Id;
        var isAdmin = tgId == _settings.AdminTelegramId;
        var state = _states.Get(chatId);
        var text = message.Text.Trim();

        if (text == "/start" || text.StartsWith("/start "))
        {
            await StartCommand(chatId, tgId, isAdmin, state, ct);
            return;
        }

        if (text == "/admin" && isAdmin)
        {
            await _bot.SendMessage(chatId, "Меню администратора", replyMarkup: BuildAdminMenu(), cancellationToken: ct);
            return;
        }

        if (state.Step == ConversationStep.AwaitingName)
        {
            state.DraftName = text;
            state.Step = ConversationStep.AwaitingPhone;
            await _bot.SendMessage(chatId, "Введите ваш телефон", cancellationToken: ct);
            return;
        }

        if (state.Step == ConversationStep.AwaitingPhone)
        {
            await _userRepository.CreateAsync(tgId, state.DraftName, text);
            state.Step = ConversationStep.None;
            await _bot.SendMessage(chatId, "Данные сохранены", replyMarkup: BuildMainMenu(isAdmin), cancellationToken: ct);
            return;
        }

        if (isAdmin && state.Step == ConversationStep.AdminAddServiceName)
        {
            state.DraftServiceName = text;
            state.Step = ConversationStep.AdminAddServicePrice;
            await _bot.SendMessage(chatId, "Введите цену услуги", cancellationToken: ct);
            return;
        }

        if (isAdmin && state.Step == ConversationStep.AdminAddServicePrice)
        {
            if (!decimal.TryParse(text.Replace(',', '.'), out var price))
            {
                await _bot.SendMessage(chatId, "Некорректная цена, попробуйте снова", cancellationToken: ct);
                return;
            }

            state.DraftServicePrice = price;
            state.Step = ConversationStep.AdminAddServiceDuration;
            await _bot.SendMessage(chatId, "Введите длительность в минутах", cancellationToken: ct);
            return;
        }

        if (isAdmin && state.Step == ConversationStep.AdminAddServiceDuration)
        {
            if (!int.TryParse(text, out var minutes) || minutes <= 0)
            {
                await _bot.SendMessage(chatId, "Некорректное число", cancellationToken: ct);
                return;
            }

            await _serviceRepository.CreateAsync(state.DraftServiceName, state.DraftServicePrice, minutes);
            state.Step = ConversationStep.None;
            await _bot.SendMessage(chatId, "Услуга добавлена", replyMarkup: BuildMainMenu(true), cancellationToken: ct);
        }
    }

    private async Task StartCommand(long chatId, long tgId, bool isAdmin, ConversationContext state, CancellationToken ct)
    {
        state.Step = ConversationStep.None;
        _states.ClearBookingData(chatId);

        var user = await _userRepository.GetByTelegramIdAsync(tgId);
        if (user == null)
        {
            state.Step = ConversationStep.AwaitingName;
            await _bot.SendMessage(chatId, "Введите ваше имя", cancellationToken: ct);
            return;
        }

        await _bot.SendMessage(chatId, "Главное меню", replyMarkup: BuildMainMenu(isAdmin), cancellationToken: ct);
    }

    private async Task OnCallback(CallbackQuery callback, CancellationToken ct)
    {
        if (callback.From == null || callback.Message == null || callback.Data == null)
            return;

        var chatId = callback.Message.Chat.Id;
        var tgId = callback.From.Id;
        var isAdmin = tgId == _settings.AdminTelegramId;
        var state = _states.Get(chatId);
        var data = callback.Data;

        await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);

        if (data == "book")
        {
            await ShowServices(chatId, ct);
            return;
        }

        if (data == "admin_add_service" && isAdmin)
        {
            state.Step = ConversationStep.AdminAddServiceName;
            await _bot.SendMessage(chatId, "Введите название услуги", cancellationToken: ct);
            return;
        }

        if (data == "admin_delete_service" && isAdmin)
        {
            await ShowServicesForDelete(chatId, ct);
            return;
        }

        if (data == "admin_list_bookings" && isAdmin)
        {
            await ShowAllBookings(chatId, ct);
            return;
        }

        if (data.StartsWith("service:"))
        {
            var id = int.Parse(data.Replace("service:", ""));
            state.SelectedServiceId = id;
            state.SelectedDate = null;
            state.SelectedTime = null;
            var days = _bookingService.GetDaysForCurrentMonth();
            await _bot.SendMessage(chatId, "Выберите день", replyMarkup: BuildCalendar(days), cancellationToken: ct);
            return;
        }

        if (data.StartsWith("date:"))
        {
            var dateStr = data.Replace("date:", "");
            state.SelectedDate = DateOnly.Parse(dateStr);
            state.SelectedTime = null;

            if (state.SelectedServiceId == null)
                return;

            var busy = await _bookingService.GetBusyTimesAsync(state.SelectedServiceId.Value, state.SelectedDate.Value);
            var free = _bookingService.GetFreeTimes(busy);

            if (free.Count == 0)
            {
                await _bot.SendMessage(chatId, "Нет свободного времени", cancellationToken: ct);
                return;
            }

            await _bot.SendMessage(chatId, "Выберите время", replyMarkup: BuildTimes(free), cancellationToken: ct);
            return;
        }

        if (data.StartsWith("time:"))
        {
            var timeStr = data.Replace("time:", "");
            state.SelectedTime = TimeOnly.Parse(timeStr);
            await _bot.SendMessage(chatId, "Подтвердите запись", replyMarkup: BuildConfirmButton(), cancellationToken: ct);
            return;
        }

        if (data == "confirm_booking")
        {
            await SaveBooking(chatId, tgId, state, ct);
            return;
        }

        if (data.StartsWith("delete_service:") && isAdmin)
        {
            var id = int.Parse(data.Replace("delete_service:", ""));
            await _serviceRepository.DeleteAsync(id);
            await _bot.SendMessage(chatId, "Услуга удалена", replyMarkup: BuildMainMenu(true), cancellationToken: ct);
            return;
        }

        if (data.StartsWith("confirm:") && isAdmin)
        {
            var id = int.Parse(data.Replace("confirm:", ""));
            await AdminAnswer(id, BookingStatus.Confirmed, ct);
            return;
        }

        if (data.StartsWith("cancel:") && isAdmin)
        {
            var id = int.Parse(data.Replace("cancel:", ""));
            await AdminAnswer(id, BookingStatus.Rejected, ct);
            return;
        }

        if (data.StartsWith("reschedule:") && isAdmin)
        {
            var id = int.Parse(data.Replace("reschedule:", ""));
            await AdminAnswer(id, BookingStatus.RescheduleRequested, ct);
        }
    }

    private async Task ShowServices(long chatId, CancellationToken ct)
    {
        var services = await _serviceRepository.GetAllAsync();
        if (services.Count == 0)
        {
            await _bot.SendMessage(chatId, "Услуг пока нет", cancellationToken: ct);
            return;
        }

        var rows = new List<InlineKeyboardButton[]>();
        foreach (var s in services)
        {
            var label = s.Name + " | " + s.Price + " руб | " + s.DurationMinutes + " мин";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(label, "service:" + s.Id) });
        }

        await _bot.SendMessage(chatId, "Выберите услугу", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
    }

    private async Task ShowServicesForDelete(long chatId, CancellationToken ct)
    {
        var services = await _serviceRepository.GetAllAsync();
        if (services.Count == 0)
        {
            await _bot.SendMessage(chatId, "Список пуст", cancellationToken: ct);
            return;
        }

        var rows = services.Select(s => new[] { InlineKeyboardButton.WithCallbackData(s.Name, "delete_service:" + s.Id) }).ToArray();
        await _bot.SendMessage(chatId, "Что удалить?", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
    }

    private async Task ShowAllBookings(long chatId, CancellationToken ct)
    {
        var bookings = await _bookingService.GetAllBookingsSortedAsync();
        if (bookings.Count == 0)
        {
            await _bot.SendMessage(chatId, "Заявок нет", cancellationToken: ct);
            return;
        }

        var text = "";
        foreach (var b in bookings)
        {
            text += "#" + b.Id + " " + b.Status + "\n";
            text += b.UserName + " " + b.UserPhone + "\n";
            text += b.ServiceName + " " + b.BookingDate.ToString("dd.MM.yyyy") + " " + b.BookingTime.ToString("HH:mm") + "\n\n";
        }

        if (text.Length > 4000)
            text = text.Substring(0, 4000);

        await _bot.SendMessage(chatId, text, cancellationToken: ct);
    }

    private async Task SaveBooking(long chatId, long tgId, ConversationContext state, CancellationToken ct)
    {
        if (state.SelectedServiceId == null || state.SelectedDate == null || state.SelectedTime == null)
        {
            await _bot.SendMessage(chatId, "Сначала выберите услугу, дату и время", cancellationToken: ct);
            return;
        }

        var user = await _userRepository.GetByTelegramIdAsync(tgId);
        if (user == null)
        {
            state.Step = ConversationStep.AwaitingName;
            await _bot.SendMessage(chatId, "Введите ваше имя", cancellationToken: ct);
            return;
        }

        try
        {
            var booking = await _bookingService.CreateBookingAsync(
                user.Id,
                state.SelectedServiceId.Value,
                state.SelectedDate.Value,
                state.SelectedTime.Value);

            _states.ClearBookingData(chatId);
            await _bot.SendMessage(chatId, "Заявка на запись в обработке, ожидайте подтверждения", cancellationToken: ct);

            var adminText = "Новая заявка\n";
            adminText += "Имя: " + booking.UserName + "\n";
            adminText += "Телефон: " + booking.UserPhone + "\n";
            adminText += "Услуга: " + booking.ServiceName + "\n";
            adminText += "Дата: " + booking.BookingDate.ToString("dd.MM.yyyy") + "\n";
            adminText += "Время: " + booking.BookingTime.ToString("HH:mm");

            await _bot.SendMessage(_settings.AdminTelegramId, adminText, replyMarkup: BuildAdminActions(booking.Id), cancellationToken: ct);
        }
        catch
        {
            await _bot.SendMessage(chatId, "Не удалось записаться, выберите другое время", cancellationToken: ct);
        }
    }

    private async Task AdminAnswer(int bookingId, BookingStatus status, CancellationToken ct)
    {
        var ok = await _bookingService.ChangeStatusAsync(bookingId, status);
        if (!ok)
            return;

        var booking = await _bookingService.GetByIdAsync(bookingId);
        if (booking == null)
            return;

        string msg;
        if (status == BookingStatus.Confirmed)
        {
            msg = "Запись подтверждена\n\n";
            msg += "Lorem ipsum dolor sit amet, consectetur adipiscing elit.\n\n";
            msg += "Услуга: " + booking.ServiceName + "\n";
            msg += "Дата: " + booking.BookingDate.ToString("dd.MM.yyyy") + "\n";
            msg += "Время: " + booking.BookingTime.ToString("HH:mm");
        }
        else if (status == BookingStatus.Rejected)
        {
            msg = "Запись отклонена";
        }
        else
        {
            msg = "Мастер просит поменять время";
        }

        await _bot.SendMessage(booking.UserTelegramId, msg, cancellationToken: ct);
    }

    private InlineKeyboardMarkup BuildMainMenu(bool isAdmin)
    {
        if (!isAdmin)
            return new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Записаться", "book") } });

        return BuildAdminMenu();
    }

    private InlineKeyboardMarkup BuildAdminMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Записаться", "book") },
            new[] { InlineKeyboardButton.WithCallbackData("Добавить услугу", "admin_add_service") },
            new[] { InlineKeyboardButton.WithCallbackData("Удалить услугу", "admin_delete_service") },
            new[] { InlineKeyboardButton.WithCallbackData("Показать заявки", "admin_list_bookings") }
        });
    }

    private InlineKeyboardMarkup BuildCalendar(List<DateOnly> days)
    {
        var rows = new List<InlineKeyboardButton[]>();
        var row = new List<InlineKeyboardButton>();

        foreach (var day in days)
        {
            row.Add(InlineKeyboardButton.WithCallbackData(day.Day.ToString(), "date:" + day.ToString("yyyy-MM-dd")));
            if (row.Count == 7)
            {
                rows.Add(row.ToArray());
                row = new List<InlineKeyboardButton>();
            }
        }

        if (row.Count > 0)
            rows.Add(row.ToArray());

        return new InlineKeyboardMarkup(rows);
    }

    private InlineKeyboardMarkup BuildTimes(List<TimeOnly> times)
    {
        var rows = times.Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.ToString("HH:mm"), "time:" + t.ToString("HH\\:mm")) }).ToArray();
        return new InlineKeyboardMarkup(rows);
    }

    private InlineKeyboardMarkup BuildConfirmButton()
    {
        return new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Записаться", "confirm_booking") } });
    }

    private InlineKeyboardMarkup BuildAdminActions(int bookingId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Подтвердить", "confirm:" + bookingId),
                InlineKeyboardButton.WithCallbackData("Отменить", "cancel:" + bookingId),
                InlineKeyboardButton.WithCallbackData("Перенести", "reschedule:" + bookingId)
            }
        });
    }
}
