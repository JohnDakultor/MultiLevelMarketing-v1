namespace modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger.Model;

public sealed record AdminCommissionLedgerPageDto(
    Guid OrganizationId,
    string Currency,
    IReadOnlyList<AdminCommissionLedgerItemDto> Items,
    AdminCommissionLedgerTotalsDto Totals,
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
