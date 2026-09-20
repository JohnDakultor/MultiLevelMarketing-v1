namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrders.Models;

public sealed record AttributedOrdersPageDto(
    IReadOnlyList<AttributedOrderSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
