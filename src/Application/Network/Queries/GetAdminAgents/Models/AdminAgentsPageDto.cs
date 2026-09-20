namespace modular_mlm.Application.Network.Queries.GetAdminAgents.Models;

public sealed record AdminAgentsPageDto(
    IReadOnlyList<AdminAgentSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
