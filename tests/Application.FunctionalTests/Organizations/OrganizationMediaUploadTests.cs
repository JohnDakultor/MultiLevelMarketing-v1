using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Organizations;

using static Infrastructure.TestApp;

public sealed class OrganizationMediaUploadTests : TestBase
{
    [Test]
    public async Task AuthorizedUploadStoresInspectedContentAndUpdatesBranding()
    {
        var organization = Organization.Create("Media", $"media-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);
        using var client = await CreateAdministratorClientAsync();
        var png = CreatePngHeader(64, 32);
        using var content = CreateUploadContent(png, "../../client-controlled.png", "image/png");

        using var response = await client.PostAsync(
            $"/api/organizations/{organization.Id}/admin/branding/assets/Logo",
            content
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var storage = GetRequiredService<TestObjectStorage>();
        storage.Uploads.Count.ShouldBe(1);
        storage.Uploads[0].Content.ShouldBe(png);
        storage
            .Uploads[0]
            .ObjectKey.ShouldStartWith($"organizations/{organization.Id:D}/branding/logo/");
        storage.Uploads[0].ObjectKey.ShouldNotContain("client-controlled");
        var persisted = await FindAsync<Organization>(organization.Id);
        persisted!.Branding.LogoUrl.ShouldBe($"https://assets.test/{storage.Uploads[0].ObjectKey}");
    }

    [Test]
    public async Task ForeignAdministratorCannotUploadBrandingAsset()
    {
        var owned = Organization.Create("Owned", $"media-owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"media-foreign-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owned);
        await AddAsync(foreign);
        await RunAsAdministratorAsync(owned.Id);
        using var client = await CreateAdministratorClientAsync();
        using var content = CreateUploadContent(CreatePngHeader(16, 16), "logo.png", "image/png");

        using var response = await client.PostAsync(
            $"/api/organizations/{foreign.Id}/admin/branding/assets/Logo",
            content
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        GetRequiredService<TestObjectStorage>().Uploads.ShouldBeEmpty();
    }

    [Test]
    public async Task MismatchedContentIsRejectedBeforeStorage()
    {
        var organization = Organization.Create(
            "Rejected Media",
            $"rejected-media-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);
        using var client = await CreateAdministratorClientAsync();
        using var content = CreateUploadContent(CreatePngHeader(16, 16), "logo.jpg", "image/jpeg");

        using var response = await client.PostAsync(
            $"/api/organizations/{organization.Id}/admin/branding/assets/Logo",
            content
        );

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        GetRequiredService<TestObjectStorage>().Uploads.ShouldBeEmpty();
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
