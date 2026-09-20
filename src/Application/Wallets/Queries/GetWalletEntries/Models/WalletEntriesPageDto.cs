namespace Application.Wallets.Queries.GetWalletEntries.Models;

public sealed record WalletEntriesPageDto(
    IReadOnlyList<WalletEntryDto> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
