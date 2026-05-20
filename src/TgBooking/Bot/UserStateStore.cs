namespace TgBooking.Bot;

public class UserStateStore
{
    private readonly Dictionary<long, ConversationContext> _states = new();

    public ConversationContext Get(long chatId)
    {
        if (!_states.ContainsKey(chatId))
            _states[chatId] = new ConversationContext();

        return _states[chatId];
    }

    public void ClearBookingData(long chatId)
    {
        var ctx = Get(chatId);
        ctx.SelectedServiceId = null;
        ctx.SelectedDate = null;
        ctx.SelectedTime = null;
    }
}
