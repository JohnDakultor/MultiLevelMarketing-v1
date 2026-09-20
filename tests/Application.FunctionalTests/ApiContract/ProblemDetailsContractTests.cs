using System.Net;
using System.Text.Json;
using modular_mlm.Web.Infrastructure;

namespace modular_mlm.Application.FunctionalTests.ApiContract;

public sealed class ProblemDetailsContractTests : TestBase
{
    [Test]
    public async Task ValidationFailureContainsStableCodeAndTraceId()
    {
        using var client = FunctionalTestSetup.CreateClient();
        using var response = await client.GetAsync(
            $"/api/organizations/{Guid.NewGuid()}/products?page=0&pageSize=0"
        );
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("errorCode")
            .GetString()
            .ShouldBe(ApiErrorCodes.ValidationFailed);
        json.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        json.RootElement.GetRawText().ShouldNotContain("StackTrace");
    }
}
