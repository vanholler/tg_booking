namespace TgBooking.Bot;

public class ConversationContext
{
    public ConversationStep Step = ConversationStep.None;
    public string DraftName = "";
    public string DraftServiceName = "";
    public decimal DraftServicePrice;
    public int? SelectedServiceId;
    public DateOnly? SelectedDate;
    public TimeOnly? SelectedTime;
}
