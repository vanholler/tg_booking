namespace TgBooking.Bot;

public enum ConversationStep
{
    None,
    AwaitingName,
    AwaitingPhone,
    AdminAddServiceName,
    AdminAddServicePrice,
    AdminAddServiceDuration
}
