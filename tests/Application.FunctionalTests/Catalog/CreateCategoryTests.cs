using modular_mlm.Application.Catalog.Commands.CreateCategory;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Catalog;

using static Infrastructure.TestApp;

public sealed class CreateCategoryTests : Infrastructure.TestBase
{
    [Test]
    public async Task CreatesNormalizedCategoryAndAuditAtomically()
    {
        var organization = await CreateOrganizationAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        var categoryId = await SendAsync(
            new CreateCategoryCommand(organization.Id, "  Health Products  ", "health-products")
        );

        var category = await FindAsync<Category>(categoryId);
        category.ShouldNotBeNull();
        category.OrganizationId.ShouldBe(organization.Id);
        category.Name.ShouldBe("Health Products");
        category.Slug.ShouldBe("health-products");
        category.IsActive.ShouldBeTrue();

        var audit = await SingleAsync<AuditLog>(entry =>
            entry.OrganizationId == organization.Id
            && entry.EntityId == categoryId
            && entry.Action == AuditCoverageMap.CategoryCreated.Action
        );
        audit.ActorUserId.ShouldBe(actorId);
        audit.EntityType.ShouldBe(AuditEntityNames.Category);
        audit.BeforeJson.ShouldBeNull();
        audit.AfterJson.ShouldNotBeNull();
        audit.AfterJson!.ShouldContain("Health Products");
        audit.AfterJson.ShouldContain("health-products");
    }

    [Test]
    public async Task RejectsDuplicateSlugInsideTheSameOrganizationWithoutWritingAnotherAudit()
    {
        var organization = await CreateOrganizationAsync();
        await RunAsAdministratorAsync(organization.Id);
        await SendAsync(new CreateCategoryCommand(organization.Id, "Health", "health"));

        await Should.ThrowAsync<ConflictException>(() =>
            SendAsync(new CreateCategoryCommand(organization.Id, "Other", "health"))
        );

        (await CountAsync<Category>(category => category.OrganizationId == organization.Id))
            .ShouldBe(1);
        (await CountAsync<AuditLog>(entry =>
            entry.OrganizationId == organization.Id
            && entry.Action == AuditCoverageMap.CategoryCreated.Action
        )).ShouldBe(1);
    }

    [Test]
    public async Task AllowsTheSameSlugInDifferentOrganizations()
    {
        var organization = await CreateOrganizationAsync();
        var otherOrganization = await CreateOrganizationAsync();
        await AddAsync(Category.Create(otherOrganization.Id, "Health", "health"));
        await RunAsAdministratorAsync(organization.Id);

        var categoryId = await SendAsync(
            new CreateCategoryCommand(organization.Id, "Health", "health")
        );

        (await FindAsync<Category>(categoryId))!.OrganizationId.ShouldBe(organization.Id);
    }

    [Test]
    public async Task RejectsInvalidCanonicalSlug()
    {
        var organization = await CreateOrganizationAsync();
        await RunAsAdministratorAsync(organization.Id);

        await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(new CreateCategoryCommand(organization.Id, "Health", "Health Products"))
        );
    }

    [Test]
    public async Task HonorsCancellationWithoutPersistingCategoryOrAudit()
    {
        var organization = await CreateOrganizationAsync();
        await RunAsAdministratorAsync(organization.Id);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            SendAsync(
                new CreateCategoryCommand(organization.Id, "Health", "health"),
                cancellation.Token
            )
        );

        (await CountAsync<Category>()).ShouldBe(0);
        (await CountAsync<AuditLog>()).ShouldBe(0);
    }

    [Test]
    public async Task ConcurrentDuplicateRequestsPersistOnlyOneCategoryAndAudit()
    {
        var organization = await CreateOrganizationAsync();
        await RunAsAdministratorAsync(organization.Id);

        async Task<Exception?> Attempt(string name)
        {
            try
            {
                await SendAsync(new CreateCategoryCommand(organization.Id, name, "health"));
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        var outcomes = await Task.WhenAll(Attempt("Health One"), Attempt("Health Two"));

        outcomes.Count(outcome => outcome is null).ShouldBe(1);
        outcomes.Count(outcome => outcome is ConflictException).ShouldBe(1);
        (await CountAsync<Category>(category => category.OrganizationId == organization.Id))
            .ShouldBe(1);
        (await CountAsync<AuditLog>(entry =>
            entry.OrganizationId == organization.Id
            && entry.Action == AuditCoverageMap.CategoryCreated.Action
        )).ShouldBe(1);
    }

    private static async Task<Organization> CreateOrganizationAsync()
    {
        var organization = Organization.Create(
            "Category Tests",
            $"category-tests-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        return organization;
    }
}
