using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.FunctionalTests.Security;

public sealed class AuthenticationHardeningTests : TestBase
{
    [Test]
    public async Task RotationAndRevocationInvalidateOldSessions()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.ExecuteInScopeAsync(async services =>
        {
            var sessions = services.GetRequiredService<IAuthenticationSessionService>();
            var now = DateTimeOffset.UtcNow;
            var first = await sessions.CreateOrRotateSessionAsync(
                userId,
                null,
                now,
                now.AddDays(1),
                CancellationToken.None
            );
            var second = await sessions.CreateOrRotateSessionAsync(
                userId,
                first,
                now.AddMinutes(1),
                now.AddDays(1),
                CancellationToken.None
            );
            (
                await sessions.IsSessionActiveAsync(
                    userId,
                    first,
                    now.AddMinutes(2),
                    CancellationToken.None
                )
            ).ShouldBeFalse();
            (
                await sessions.IsSessionActiveAsync(
                    userId,
                    second,
                    now.AddMinutes(2),
                    CancellationToken.None
                )
            ).ShouldBeTrue();
            var listed = await sessions.GetSessionsAsync(userId, second, CancellationToken.None);
            listed.Count.ShouldBe(2);
            listed.Single(session => session.Id == second).IsCurrent.ShouldBeTrue();
            (
                await sessions.RevokeSessionAsync(
                    userId,
                    second,
                    now.AddMinutes(3),
                    "test",
                    CancellationToken.None
                )
            ).ShouldBeTrue();
            (
                await sessions.RevokeSessionAsync(
                    userId,
                    second,
                    now.AddMinutes(4),
                    "test",
                    CancellationToken.None
                )
            ).ShouldBeFalse();
            return true;
        });
    }
}
