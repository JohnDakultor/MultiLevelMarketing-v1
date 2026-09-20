using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Storage;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.Storage;

public sealed class S3ObjectStorageTests
{
    [Test]
    public async Task PutSendsExpectedMetadataAndReturnsPublicObject()
    {
        PutObjectRequest? captured = null;
        var client = new Mock<IAmazonS3>();
        client
            .Setup(value =>
                value.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>())
            )
            .Callback<PutObjectRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new PutObjectResponse { ETag = "etag-1", VersionId = "version-1" });
        var storage = CreateStorage(client.Object);
        await using var content = new MemoryStream([1, 2, 3]);
        var upload = new ObjectUploadRequest(
            "organizations/tenant/branding/logo/asset.png",
            "image/png",
            content.Length,
            content
        );

        var result = await storage.PutAsync(upload, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.BucketName.ShouldBe("phase-seven-assets");
        captured.Key.ShouldBe(upload.ObjectKey);
        captured.ContentType.ShouldBe("image/png");
        captured.Headers.CacheControl.ShouldBe("public,max-age=31536000,immutable");
        captured.AutoCloseStream.ShouldBeFalse();
        result.Url.AbsoluteUri.ShouldBe(
            "https://cdn.example/assets/organizations/tenant/branding/logo/asset.png"
        );
        result.ETag.ShouldBe("etag-1");
        result.VersionId.ShouldBe("version-1");
    }

    [Test]
    public async Task DeleteUsesConfiguredBucketAndValidatedObjectKey()
    {
        var client = new Mock<IAmazonS3>();
        client
            .Setup(value =>
                value.DeleteObjectAsync(
                    "phase-seven-assets",
                    "organizations/tenant/branding/logo/asset.png",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new DeleteObjectResponse());
        var storage = CreateStorage(client.Object);

        await storage.DeleteAsync(
            "organizations/tenant/branding/logo/asset.png",
            CancellationToken.None
        );

        client.VerifyAll();
    }

    [Test]
    public async Task ProviderFailureIsTranslatedWithoutExposingCredentials()
    {
        var client = new Mock<IAmazonS3>();
        client
            .Setup(value =>
                value.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new AmazonS3Exception("provider failed for secret-value"));
        var storage = CreateStorage(client.Object);
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

    private static S3ObjectStorage CreateStorage(IAmazonS3 client) =>
        new(
            client,
            Options.Create(
                new S3ObjectStorageOptions
                {
                    Enabled = true,
                    Region = "ap-southeast-1",
                    BucketName = "phase-seven-assets",
                    PublicBaseUrl = "https://cdn.example/assets",
                    AccessKey = "test-access",
                    SecretKey = "test-secret",
                }
            )
        );
}
