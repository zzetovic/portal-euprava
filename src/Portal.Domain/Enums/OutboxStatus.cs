namespace Portal.Domain.Enums;

public enum OutboxStatus
{
    Pending,
    Processing,
    Done,
    Failed,
    DeadLetter
}
