namespace modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary.Models;

public sealed record AgentProductSalesPageDto(
    IReadOnlyList<AgentProductSalesSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
