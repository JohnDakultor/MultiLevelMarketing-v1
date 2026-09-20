namespace modular_mlm.Application.Common.Models;

public sealed record PagedResponse<TItem>
{
    public IReadOnlyList<TItem> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public PagedResponse(IEnumerable<TItem> items, int page, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (page < 1)
            throw new ArgumentOutOfRangeException(
                nameof(page),
                page,
                "Page must be greater than or equal to 1."
            );

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                pageSize,
                "PageSize must be greater than or equal to 1."
            );

        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(totalCount),
                totalCount,
                "TotalCount cannot be negative."
            );

        Items = items.ToArray();
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;

        TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
