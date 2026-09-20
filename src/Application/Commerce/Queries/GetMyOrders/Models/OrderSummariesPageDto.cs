using modular_mlm.Application.Commerce.Queries.GetMyOrders.Models;

namespace modular_mlm.Application.Commerce.Queries.GetMyOrders;

public sealed record OrderSummariesPageDto(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<OrderSummaryDto> Items
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
