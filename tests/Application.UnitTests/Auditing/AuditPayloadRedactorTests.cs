using System.Text.Json;
using modular_mlm.Infrastructure.Auditing;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Auditing;

public sealed class AuditPayloadRedactorTests
{
    private readonly AuditPayloadRedactor _sut = new();

    [Test]
    public void RedactJsonRedactsSensitivePropertiesAtEveryDepth()
    {
        const string json = """
            {
              "name": "safe",
              "password": "first-secret",
              "nested": {
                "raw_token": "second-secret",
                "items": [{ "protectedAccountNumber": "third-secret", "status": "ok" }]
              }
            }
            """;

        var result = _sut.RedactJson(json);

        result.ShouldNotBeNull();
        result.ShouldContain("safe");
        result.ShouldContain("ok");
        result.ShouldNotContain("first-secret");
        result.ShouldNotContain("second-secret");
        result.ShouldNotContain("third-secret");
        CountMarker(result!).ShouldBe(3);
    }

    [Test]
    public void SerializeAndRedactHandlesObjectsWithoutLeakingCredentials()
    {
        var result = _sut.SerializeAndRedact(
            new
            {
                Status = "Verified",
                Credentials = new { UserName = "private-user", SecretKey = "private-key" },
            }
        );

        var payload = result ?? throw new AssertionException("Expected serialized payload.");
        payload.ShouldContain("Verified");
        payload.ShouldNotContain("private-user");
        payload.ShouldNotContain("private-key");
    }

    [Test]
    public void RedactJsonFailsClosedForMalformedOrOversizedPayloads()
    {
        var malformed = _sut.RedactJson("{ not-json }");
        var oversized = _sut.RedactJson($"\"{new string('x', 70_000)}\"");

        var malformedPayload =
            malformed ?? throw new AssertionException("Expected malformed marker.");
        var oversizedPayload =
            oversized ?? throw new AssertionException("Expected oversized marker.");
        malformedPayload.ShouldContain(AuditPayloadRedactor.InvalidPayloadMarker);
        oversizedPayload.ShouldContain(AuditPayloadRedactor.OversizedPayloadMarker);
        malformedPayload.ShouldNotContain("not-json");
        oversizedPayload.ShouldNotContain(new string('x', 100));
    }

    [Test]
    public void RedactJsonPreservesNullAndBlankAsAbsentPayloads()
    {
        _sut.RedactJson(null).ShouldBeNull();
        _sut.RedactJson("  ").ShouldBeNull();
        _sut.SerializeAndRedact(null).ShouldBeNull();
    }

    [Test]
    public void RedactTextRemovesNamedSecretsAndBearerCredentials()
    {
        var result = _sut.RedactText(
            "Rejected because token=raw-token and Authorization: Bearer raw-bearer"
        );

        var redacted = result ?? throw new AssertionException("Expected redacted text.");
        redacted.ShouldContain(AuditPayloadRedactor.RedactedMarker);
        redacted.ShouldNotContain("raw-token");
        redacted.ShouldNotContain("raw-bearer");
    }

    private static int CountMarker(string json)
    {
        using var document = JsonDocument.Parse(json);
        return CountMarker(document.RootElement);
    }

    private static int CountMarker(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => element
                .EnumerateObject()
                .Sum(property => CountMarker(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Sum(CountMarker),
            JsonValueKind.String when element.GetString() == AuditPayloadRedactor.RedactedMarker =>
                1,
            _ => 0,
        };
}
