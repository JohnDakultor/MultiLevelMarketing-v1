namespace modular_mlm.Application.Wallets.Queries.GetAdminWallets.Models;

public sealed record AdminWalletsPageDto(
    Guid OrganizationId,
    IReadOnlyList<AdminWalletSummaryDto> Items,
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
