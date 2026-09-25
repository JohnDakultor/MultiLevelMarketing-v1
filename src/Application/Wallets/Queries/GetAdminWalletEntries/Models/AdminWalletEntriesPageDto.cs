namespace modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries.Models;

public sealed record AdminWalletEntriesPageDto(
    Guid OrganizationId,
    Guid AgentId,
    Guid WalletId,
    string AgentCode,
    string Currency,
    IReadOnlyList<AdminWalletEntryDto> Items,
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
