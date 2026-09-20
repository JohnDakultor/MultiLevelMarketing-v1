using modular_mlm.Application.Referrals.Commands.CreateProductReferralLink;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Referrals;

using static Infrastructure.TestApp;

public sealed class ProductReferralLinkTests : TestBase
{
    [Test]
    public async Task ActiveAgentCanCreateSafeProductReferralLink()
    {
        var organization = Organization.Create("Referral", $"referral-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Agent1234!",
            [Roles.Agent]
        );
        var agent = Agent.Apply(
            organization.Id,
            userId,
            "AG-1",
            "SAFE-CODE",
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        await AddAsync(agent);
        var product = Product.Create(
            organization.Id,
            Guid.NewGuid(),
            "Green Pack",
            "green-pack",
            "Description"
        );
        product.AddVariant("GREEN-1", 100m, 20m, 10);
        product.Publish();
        await AddAsync(product);

        var result = await SendAsync(
            new CreateProductReferralLinkCommand(organization.Id, product.Id)
        );

        result.ProductId.ShouldBe(product.Id);
        result.ReferralCode.ShouldBe(agent.ReferralCode);
        result.CanonicalUrl.ShouldContain("/r/SAFE-CODE/products/green-pack");
        result.QrPayload.ShouldBe(result.CanonicalUrl);
        result.QrPayload.ToLowerInvariant().ShouldNotContain("<script");
    }

    [Test]
    public async Task DraftOrForeignProductCannotBeShared()
    {
        var organization = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        await AddAsync(foreign);
        var userId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Agent1234!",
            [Roles.Agent]
        );
        var agent = Agent.Apply(organization.Id, userId, "AG-1", "REF-1", DateTimeOffset.UtcNow);
        agent.Activate(DateTimeOffset.UtcNow);
        await AddAsync(agent);
        var draft = Product.Create(
            organization.Id,
            Guid.NewGuid(),
            "Draft",
            "draft",
            "Description"
        );
        var foreignProduct = Product.Create(
            foreign.Id,
            Guid.NewGuid(),
            "Foreign",
            "foreign",
            "Description"
        );
        foreignProduct.AddVariant("FOREIGN-1", 10m, 1m, 1);
        foreignProduct.Publish();
        await AddAsync(draft);
        await AddAsync(foreignProduct);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(new CreateProductReferralLinkCommand(organization.Id, draft.Id))
        );
        await Should.ThrowAsync<KeyNotFoundException>(() =>
            SendAsync(new CreateProductReferralLinkCommand(organization.Id, foreignProduct.Id))
        );
    }
}
