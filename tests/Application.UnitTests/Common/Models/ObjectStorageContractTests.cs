using modular_mlm.Application.Common.Models;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Common.Models;

public sealed class ObjectStorageContractTests
{
    [Test]
    public void UploadRequestNormalizesTrustedMetadata()
    {
        using var content = new MemoryStream([1, 2, 3]);

        var request = new ObjectUploadRequest(
            " organizations/id/branding/logo/image.png ",
            " IMAGE/PNG ",
            content.Length,
            content
        );

        request.ObjectKey.ShouldBe("organizations/id/branding/logo/image.png");
        request.ContentType.ShouldBe("image/png");
        request.ContentLength.ShouldBe(3);
        request.Content.ShouldBeSameAs(content);
    }

    [TestCase("")]
    [TestCase("/rooted.png")]
    [TestCase("trailing/")]
    [TestCase("organizations/../secret")]
    [TestCase("organizations//logo.png")]
    [TestCase("organizations\\logo.png")]
    public void UploadRequestRejectsUnsafeObjectKeys(string objectKey)
    {
        using var content = new MemoryStream([1]);

        Should.Throw<ArgumentException>(() =>
            new ObjectUploadRequest(objectKey, "image/png", 1, content)
        );
    }

    [TestCase("")]
    [TestCase("image")]
    [TestCase("image/png/extra")]
    [TestCase("image/png; charset=utf-8")]
    [TestCase("image/ png")]
    public void UploadRequestRejectsInvalidContentTypes(string contentType)
    {
        using var content = new MemoryStream([1]);

        Should.Throw<ArgumentException>(() =>
            new ObjectUploadRequest("branding/image.png", contentType, 1, content)
        );
    }

    [Test]
    public void UploadRequestRejectsUnreadableStreams()
    {
        using var content = new WriteOnlyStream();

        Should.Throw<ArgumentException>(() =>
            new ObjectUploadRequest("branding/image.png", "image/png", 1, content)
        );
    }

    [Test]
    public void StoredObjectRejectsUrlsWithEmbeddedCredentials()
    {
        Should.Throw<ArgumentException>(() =>
            new StoredObject(
                "branding/image.png",
                new Uri("https://access:secret@cdn.example.com/image.png"),
                "image/png",
                10
            )
        );
    }

    [Test]
    public void StoredObjectNormalizesOptionalProviderMetadata()
    {
        var stored = new StoredObject(
            "branding/image.png",
            new Uri("https://cdn.example.com/image.png"),
            "image/png",
            10,
            " etag ",
            " version-1 "
        );

        stored.ETag.ShouldBe("etag");
        stored.VersionId.ShouldBe("version-1");
    }

    private sealed class WriteOnlyStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
