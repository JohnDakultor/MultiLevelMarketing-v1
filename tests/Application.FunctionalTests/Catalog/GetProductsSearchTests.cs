using modular_mlm.Application.Catalog.Commands.CreateCategory;
using modular_mlm.Application.Catalog.Commands.CreateProduct;
using modular_mlm.Application.Catalog.Commands.PublishProduct;
using modular_mlm.Application.Catalog.Queries.GetProducts;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Catalog;

using static Infrastructure.TestApp;

[NonParallelizable]
public sealed class GetProductsSearchTests : TestBase
{
    [Test]
    public async Task FiltersSortsAndPaginatesAtTheDatabase()
    {
        var organization = Organization.Create("Catalog", $"catalog-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);
        var wellness = await SendAsync(
            new CreateCategoryCommand(organization.Id, "Wellness", "wellness")
        );
        var beauty = await SendAsync(
            new CreateCategoryCommand(organization.Id, "Beauty", "beauty")
        );
        await CreatePublishedProduct(organization.Id, wellness, "Daily Greens", "greens", 500m, 8);
        await CreatePublishedProduct(organization.Id, wellness, "Green Tea", "tea", 250m, 0);
        await CreatePublishedProduct(organization.Id, beauty, "Face Cream", "cream", 750m, 4);

        var search = await SendAsync(
            new GetProductsQuery(organization.Id, Search: "GREEN", Sort: ProductSort.PriceAscending)
        );
        search.TotalCount.ShouldBe(2);
        search.Items.Select(item => item.Price).ShouldBe([250m, 500m]);

        var categoryAndPrice = await SendAsync(
            new GetProductsQuery(
                organization.Id,
                CategorySlug: "wellness",
                MinimumPrice: 300m,
                MaximumPrice: 600m
            )
        );
        categoryAndPrice.Items.Single().Slug.ShouldBe("greens");

        var outOfStock = await SendAsync(
            new GetProductsQuery(organization.Id, Availability: ProductAvailability.OutOfStock)
        );
        outOfStock.Items.Single().Slug.ShouldBe("tea");

        var page = await SendAsync(
            new GetProductsQuery(
                organization.Id,
                Page: 2,
                PageSize: 1,
                Sort: ProductSort.PriceAscending
            )
        );
        page.TotalCount.ShouldBe(3);
        page.TotalPages.ShouldBe(3);
        page.Items.Single().Price.ShouldBe(500m);
    }

    private static async Task CreatePublishedProduct(
        Guid organizationId,
        Guid categoryId,
        string name,
        string slug,
        decimal price,
        int stock
    )
    {
        var productId = await SendAsync(
            new CreateProductCommand(
                organizationId,
                categoryId,
                name,
                slug,
                $"Description for {name}",
                $"SKU-{slug}",
                price,
                10m,
                stock
            )
        );
        await SendAsync(new PublishProductCommand(organizationId, productId));
    }
}
