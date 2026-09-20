namespace modular_mlm.Application.Network.Queries.GetAgentApplications.Models;

public sealed record AgentApplicationsPageDto(
    IReadOnlyList<AgentApplicationSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
