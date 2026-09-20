namespace modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages.Models;

public sealed record DeadLetterMessagesPageDto(
    IReadOnlyList<DeadLetterMessageDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
