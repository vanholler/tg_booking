using System.Collections.Concurrent;

namespace TgBooking.Bot;

public class UserStateStore
{
    private readonly ConcurrentDictionary<long, ConversationContext> _states = new();

    public ConversationContext Get(long chatId)
    {
        if (chatId == 0)
            throw new ArgumentOutOfRangeException(nameof(chatId), chatId, "Идентификатор чата не может быть нулевым.");

        return _states.GetOrAdd(chatId, _ => new ConversationContext());
    }

    public void ClearBookingData(long chatId)
    {
        if (chatId == 0)
            throw new ArgumentOutOfRangeException(nameof(chatId), chatId, "Идентификатор чата не может быть нулевым.");

        var ctx = Get(chatId);
        ctx.SelectedServiceId = null;
        ctx.SelectedDate = null;
        ctx.SelectedTime = null;
    }
}
