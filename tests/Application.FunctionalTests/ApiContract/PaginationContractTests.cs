using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.FunctionalTests.ApiContract;

public sealed class PaginationContractTests
{
    [TestCase(0, 0, false, false)]
    [TestCase(21, 2, true, false)]
    [TestCase(41, 3, true, true)]
    public void SharedPaginationCalculatesStableMetadata(
        int totalCount,
        int expectedPages,
        bool expectedNext,
        bool expectedPrevious
    )
    {
        var page = totalCount > 40 ? 2 : 1;
        var result = new PagedResponse<int>([], page, 20, totalCount);
        result.TotalPages.ShouldBe(expectedPages);
        result.HasNextPage.ShouldBe(expectedNext);
        result.HasPreviousPage.ShouldBe(expectedPrevious);
    }
}
