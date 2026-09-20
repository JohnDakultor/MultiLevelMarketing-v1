using System.Linq.Expressions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Domain.Constants;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Application.FunctionalTests.Infrastructure;

public static class TestApp
{
    private static string? _userId;
    private static List<string>? _roles;
    private static Guid? _organizationId;

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }

    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await mediator.Send(request);
    }

    public static string? GetUserId() => _userId;

    public static List<string>? GetRoles() => _roles;

    public static Guid? GetOrganizationId() => _organizationId;

    public static async Task<string> RunAsDefaultUserAsync()
    {
        return await RunAsUserAsync("test@local", "Testing1234!", []);
    }

    public static async Task<string> RunAsAdministratorAsync(Guid? organizationId = null)
    {
        var userId = await RunAsUserAsync(
            "administrator@local",
            "Administrator1234!",
            [Roles.Administrator]
        );
        if (organizationId.HasValue)
        {
            using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var user = await userManager.FindByIdAsync(userId);
            user!.OrganizationId = organizationId;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException("Could not associate test administrator.");
        }
        _organizationId = organizationId;
        return userId;
    }

    public static async Task<string> RunAsUserAsync(
        string userName,
        string password,
        string[] roles
    )
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);

        if (roles.Length > 0)
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in roles)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            await userManager.AddToRolesAsync(user, roles);
        }

        if (result.Succeeded)
        {
            _userId = user.Id;
            _roles = [.. roles];
            return _userId;
        }

        var errors = string.Join(Environment.NewLine, result.ToApplicationResult().Errors);

        throw new Exception($"Unable to create {userName}.{Environment.NewLine}{errors}");
    }

    public static async Task ResetState()
    {
        if (FunctionalTestSetup.DbResetter is not null)
        {
            await FunctionalTestSetup.DbResetter.ResetAsync();
        }

        _userId = null;
        _roles = null;
        _organizationId = null;
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestAdministratorInvitationDelivery>().Reset();
        scope.ServiceProvider.GetRequiredService<TestPaymentGateway>().Reset();
        scope.ServiceProvider.GetRequiredService<TestPayoutProvider>().Reset();
        scope.ServiceProvider.GetRequiredService<TestObjectStorage>().Reset();
    }

    public static TService GetRequiredService<TService>()
        where TService : notnull
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TService>();
    }

    public static async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> operation)
    {
        await using var scope = FunctionalTestSetup.ScopeFactory.CreateAsyncScope();
        return await operation(scope.ServiceProvider);
    }

    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.FindAsync<TEntity>(keyValues);
    }

    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Add(entity);

        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAsync<TEntity>()
        where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Set<TEntity>().CountAsync();
    }

    public static async Task<int> CountAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<TEntity>().CountAsync(predicate);
    }

    public static async Task<TEntity> SingleAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate
    )
        where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Set<TEntity>().AsNoTracking().SingleAsync(predicate);
    }
}
