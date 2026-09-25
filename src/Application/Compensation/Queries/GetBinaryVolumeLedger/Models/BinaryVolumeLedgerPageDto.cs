namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger.Models;

public sealed record BinaryVolumeLedgerPageDto(
    Guid OrganizationId,
    Guid AgentId,
    IReadOnlyList<BinaryVolumeLedgerItemDto> Items,
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
