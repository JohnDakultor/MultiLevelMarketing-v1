using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using modular_mlm.Application.Catalog.Commands.CreateCategory;
using modular_mlm.Application.Catalog.Commands.CreateProduct;
using modular_mlm.Application.Catalog.Commands.PublishProduct;
using modular_mlm.Application.Catalog.Queries.GetProductBySlug;
using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Catalog;

using static Infrastructure.TestApp;

[NonParallelizable]
public sealed class ProductImageUploadTests : TestBase
{
    [Test]
    public async Task AdministratorCanUploadReplaceAndRemoveStorefrontProductImage()
    {
        var (organization, productId, slug) = await CreateProductAsync("Product Media");
        using var client = await CreateAdministratorClientAsync();

        using var first = CreateUploadContent(CreatePngHeader(640, 480), "product.png", "image/png");
        using var firstResponse = await client.PostAsync(
            $"/api/organizations/{organization.Id}/admin/products/{productId}/image",
            first
        );
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var storage = GetRequiredService<TestObjectStorage>();
        storage.Uploads.Count.ShouldBe(1);
        var firstKey = storage.Uploads[0].ObjectKey;
        firstKey.ShouldStartWith(
            $"organizations/{organization.Id:D}/products/{productId:D}/images/"
        );
        var persisted = await FindAsync<Product>(productId);
        persisted!.DefaultImageObjectKey.ShouldBe(firstKey);
        persisted.DefaultImageUrl.ShouldBe($"https://assets.test/{firstKey}");

        await SendAsync(new PublishProductCommand(organization.Id, productId));
        var storefrontProduct = await SendAsync(new GetProductBySlugQuery(organization.Id, slug));
        storefrontProduct.DefaultImageUrl.ShouldBe($"https://assets.test/{firstKey}");

        using var replacement = CreateUploadContent(
            CreatePngHeader(800, 800),
            "replacement.png",
            "image/png"
        );
        using var replacementResponse = await client.PostAsync(
            $"/api/organizations/{organization.Id}/admin/products/{productId}/image",
            replacement
        );
        replacementResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        storage.Uploads.Count.ShouldBe(2);
        storage.DeletedObjectKeys.ShouldContain(firstKey);
        var replacementKey = storage.Uploads[1].ObjectKey;

        using var removeResponse = await client.DeleteAsync(
            $"/api/organizations/{organization.Id}/admin/products/{productId}/image"
        );
        removeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        storage.DeletedObjectKeys.ShouldContain(replacementKey);
        var removed = await FindAsync<Product>(productId);
        removed!.DefaultImageUrl.ShouldBeNull();
        removed.DefaultImageObjectKey.ShouldBeNull();
    }

    [Test]
    public async Task ProductImageUploadIsTenantIsolated()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owned);
        var (foreign, productId, _) = await CreateProductAsync("Foreign");
        await SetCurrentOrganizationAsync(owned.Id);
        using var client = await CreateAdministratorClientAsync();
        using var content = CreateUploadContent(CreatePngHeader(32, 32), "product.png", "image/png");

        using var response = await client.PostAsync(
            $"/api/organizations/{foreign.Id}/admin/products/{productId}/image",
            content
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        GetRequiredService<TestObjectStorage>().Uploads.ShouldBeEmpty();
    }

    [Test]
    public async Task ProductImageContentMustMatchItsDeclaredTypeAndExtension()
    {
        var (organization, productId, _) = await CreateProductAsync("Rejected Product Media");
        using var client = await CreateAdministratorClientAsync();
        using var content = CreateUploadContent(CreatePngHeader(32, 32), "product.jpg", "image/jpeg");

        using var response = await client.PostAsync(
            $"/api/organizations/{organization.Id}/admin/products/{productId}/image",
            content
        );

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        GetRequiredService<TestObjectStorage>().Uploads.ShouldBeEmpty();
    }

    private static async Task<(Organization Organization, Guid ProductId, string Slug)> CreateProductAsync(
        string name
    )
    {
        var organization = Organization.Create(name, $"product-media-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);
        var categoryId = await SendAsync(
            new CreateCategoryCommand(organization.Id, "Products", $"products-{Guid.NewGuid():N}")
        );
        var slug = $"image-product-{Guid.NewGuid():N}";
        var productId = await SendAsync(
            new CreateProductCommand(
                organization.Id,
                categoryId,
                "Image product",
                slug,
                "Product image integration test",
                $"SKU-{Guid.NewGuid():N}",
                100m,
                10m,
                5
            )
        );
        return (organization, productId, slug);
    }

    private static MultipartFormDataContent CreateUploadContent(
        byte[] bytes,
        string fileName,
        string contentType
    )
    {
        var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", fileName);
        return multipart;
    }

    private static byte[] CreatePngHeader(int width, int height)
    {
        var bytes = new byte[24];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(bytes, 0);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(16, 4),
            (uint)width
        );
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(20, 4),
            (uint)height
        );
        return bytes;
    }

    private static async Task<HttpClient> CreateAdministratorClientAsync()
    {
        var client = FunctionalTestSetup.CreateClient();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=false&useSessionCookies=false",
            new { email = "administrator@local", password = "Administrator1234!" }
        );
        loginResponse.EnsureSuccessStatusCode();
        await using var stream = await loginResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            document.RootElement.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("Login did not return an access token.")
        );
        return client;
    }
}
