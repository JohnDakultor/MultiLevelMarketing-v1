namespace modular_mlm.Application.Network.Queries.GetFilteredDownline.Models;

public sealed record FilteredDownlinePageDto(
    Guid OrganizationId,
    Guid RootAgentId,
    IReadOnlyList<FilteredDownlineAgentDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
