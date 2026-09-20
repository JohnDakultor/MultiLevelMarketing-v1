namespace modular_mlm.Application.Commerce.Queries.GetAdminOrders.Models;

public sealed record AdminOrdersPageDto(
    IReadOnlyList<AdminOrderSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
