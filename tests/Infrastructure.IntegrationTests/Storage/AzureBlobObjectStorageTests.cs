using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Storage;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.Storage;

public sealed class AzureBlobObjectStorageTests
{
    [Test]
    public async Task PutSendsExpectedHeadersAndReturnsPublicObject()
    {
        BlobUploadOptions? captured = null;
        var blob = new Mock<BlobClient>();
        blob.Setup(value =>
                value.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<Stream, BlobUploadOptions, CancellationToken>(
                (_, options, _) => captured = options
            )
            .ReturnsAsync(
                Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(
                        new ETag("etag-1"),
                        DateTimeOffset.UtcNow,
                        [],
                        "version-1",
                        null,
                        null,
                        0
                    ),
                    Mock.Of<Response>()
                )
            );
        var container = new Mock<BlobContainerClient>();
        container
            .Setup(value =>
                value.GetBlobClient("organizations/tenant/branding/logo/asset.png")
            )
            .Returns(blob.Object);
        var storage = CreateStorage(container.Object);
        await using var content = new MemoryStream([1, 2, 3]);
        var upload = new ObjectUploadRequest(
            "organizations/tenant/branding/logo/asset.png",
            "image/png",
            content.Length,
            content
        );

        var result = await storage.PutAsync(upload, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.HttpHeaders.ContentType.ShouldBe("image/png");
        captured.HttpHeaders.CacheControl.ShouldBe("public,max-age=31536000,immutable");
        result.Url.AbsoluteUri.ShouldBe(
            "https://cdn.example/assets/organizations/tenant/branding/logo/asset.png"
        );
        result.ETag.ShouldBe("etag-1");
        result.VersionId.ShouldBe("version-1");
    }

    [Test]
    public async Task DeleteUsesValidatedObjectKeyAndIncludesSnapshots()
    {
        var blob = new Mock<BlobClient>();
        blob.Setup(value =>
                value.DeleteIfExistsAsync(
                    DeleteSnapshotsOption.IncludeSnapshots,
                    null,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        var container = new Mock<BlobContainerClient>();
        container
            .Setup(value =>
                value.GetBlobClient("organizations/tenant/branding/logo/asset.png")
            )
            .Returns(blob.Object);
        var storage = CreateStorage(container.Object);

        await storage.DeleteAsync(
            "organizations/tenant/branding/logo/asset.png",
            CancellationToken.None
        );

        blob.VerifyAll();
    }

    [Test]
    public async Task ProviderFailureIsTranslatedWithoutExposingCredentials()
    {
        var blob = new Mock<BlobClient>();
        blob.Setup(value =>
                value.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new RequestFailedException("provider failed for secret-value"));
        var container = new Mock<BlobContainerClient>();
        container.Setup(value => value.GetBlobClient("safe/key.png")).Returns(blob.Object);
        var storage = CreateStorage(container.Object);
        await using var content = new MemoryStream([1]);

        var exception = await Should.ThrowAsync<ObjectStorageException>(() =>
            storage.PutAsync(
                new ObjectUploadRequest("safe/key.png", "image/png", 1, content),
                CancellationToken.None
            )
        );

        exception.Message.ShouldBe("The object could not be stored.");
        exception.Message.ShouldNotContain("secret-value");
    }

    [Test]
    public async Task PutFallsBackToContainerUriWhenPublicBaseUrlIsNotConfigured()
    {
        var blob = new Mock<BlobClient>();
        blob.Setup(value =>
                value.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(
                        new ETag("etag-1"),
                        DateTimeOffset.UtcNow,
                        [],
                        null,
                        null,
                        null,
                        0
                    ),
                    Mock.Of<Response>()
                )
            );
        var container = new Mock<BlobContainerClient>();
        container.SetupGet(value => value.Uri).Returns(
            new Uri("https://account.blob.core.windows.net/marketplace-assets")
        );
        container.Setup(value => value.GetBlobClient("safe/key.png")).Returns(blob.Object);
        var storage = new AzureBlobObjectStorage(
            container.Object,
            Options.Create(new AzureBlobObjectStorageOptions { Enabled = true })
        );
        await using var content = new MemoryStream([1]);

        var result = await storage.PutAsync(
            new ObjectUploadRequest("safe/key.png", "image/png", 1, content),
            CancellationToken.None
        );

        result.Url.AbsoluteUri.ShouldBe(
            "https://account.blob.core.windows.net/marketplace-assets/safe/key.png"
        );
    }

    private static AzureBlobObjectStorage CreateStorage(BlobContainerClient container) =>
        new(
            container,
            Options.Create(
                new AzureBlobObjectStorageOptions
                {
                    Enabled = true,
                    PublicBaseUrl = "https://cdn.example/assets",
                }
            )
        );
}
