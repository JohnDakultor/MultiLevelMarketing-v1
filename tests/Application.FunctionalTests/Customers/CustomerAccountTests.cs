using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Customers.Commands.AddCustomerAddress;
using modular_mlm.Application.Customers.Commands.RemoveCustomerAddress;
using modular_mlm.Application.Customers.Commands.UpdateCurrentCustomerProfile;
using modular_mlm.Application.Customers.Commands.UpdateCustomerAddress;
using modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile;
using modular_mlm.Application.Customers.Queries.GetCustomerAddresses;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Customers;

using static Infrastructure.TestApp;

public sealed class CustomerAccountTests : TestBase
{
    [Test]
    public async Task FirstCustomerRequestProvisionsProfileAndCustomerRole()
    {
        var organization = Organization.Create(
            "Customer Provisioning",
            $"customer-provisioning-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var email = $"new-customer-{Guid.NewGuid():N}@local";
        var userId = await RunAsUserAsync(email, "Testing1234!", []);

        var profile = await SendAsync(new GetCurrentCustomerProfileQuery(organization.Id));

        profile.OrganizationId.ShouldBe(organization.Id);
        profile.DisplayName.ShouldBe(email[..email.IndexOf('@')]);
        (
            await CountAsync<CustomerProfile>(candidate =>
                candidate.OrganizationId == organization.Id && candidate.UserId == userId
            )
        ).ShouldBe(1);
        await ExecuteInScopeAsync(async services =>
        {
            var users =
                services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<modular_mlm.Infrastructure.Identity.ApplicationUser>>();
            var identity = await users.FindByIdAsync(userId);
            identity.ShouldNotBeNull();
            (
                await users.IsInRoleAsync(identity, modular_mlm.Domain.Constants.Roles.Customer)
            ).ShouldBeTrue();
            return true;
        });
    }

    [Test]
    public async Task CurrentCustomerCanReadAndUpdateOnlyTheirProfile()
    {
        var data = await CreateCustomerAsync("profile-owner");

        var updated = await SendAsync(
            new UpdateCurrentCustomerProfileCommand(data.Organization.Id, "Updated Customer")
        );
        var current = await SendAsync(new GetCurrentCustomerProfileQuery(data.Organization.Id));

        updated.Id.ShouldBe(data.Customer.Id);
        current.Id.ShouldBe(data.Customer.Id);
        current.DisplayName.ShouldBe("Updated Customer");
    }

    [Test]
    public async Task FirstAddressBecomesDefaultAndExplicitDefaultIsListedFirst()
    {
        var data = await CreateCustomerAsync("address-default");
        var firstId = await AddAddressAsync(data.Organization.Id, "Home", false);
        var secondId = await AddAddressAsync(data.Organization.Id, "Office", true);

        var addresses = await SendAsync(new GetCustomerAddressesQuery(data.Organization.Id));
        var profile = await SendAsync(new GetCurrentCustomerProfileQuery(data.Organization.Id));

        addresses.Count.ShouldBe(2);
        addresses[0].Id.ShouldBe(secondId);
        addresses[0].IsDefault.ShouldBeTrue();
        addresses.Single(address => address.Id == firstId).IsDefault.ShouldBeFalse();
        profile.DefaultAddressId.ShouldBe(secondId);
    }

    [Test]
    public async Task RemovingDefaultAddressPromotesOldestRemainingActiveAddress()
    {
        var data = await CreateCustomerAsync("address-removal");
        var firstId = await AddAddressAsync(data.Organization.Id, "Home", false);
        var secondId = await AddAddressAsync(data.Organization.Id, "Office", true);

        await SendAsync(new RemoveCustomerAddressCommand(data.Organization.Id, secondId));

        var addresses = await SendAsync(new GetCustomerAddressesQuery(data.Organization.Id));
        var profile = await SendAsync(new GetCurrentCustomerProfileQuery(data.Organization.Id));
        var archived = await FindAsync<CustomerAddress>(secondId);

        addresses.Count.ShouldBe(1);
        addresses.Single().Id.ShouldBe(firstId);
        addresses.Single().IsDefault.ShouldBeTrue();
        profile.DefaultAddressId.ShouldBe(firstId);
        archived.ShouldNotBeNull();
        archived.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task CustomerCannotUpdateOrRemoveAnotherCustomersAddress()
    {
        var owner = await CreateCustomerAsync("address-owner");
        var foreignAddressId = await AddAddressAsync(owner.Organization.Id, "Private", false);

        var intruderUserId = await RunAsUserAsync(
            $"address-intruder-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        await AddAsync(
            CustomerProfile.Create(owner.Organization.Id, intruderUserId, "Address Intruder")
        );

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            SendAsync(
                new UpdateCustomerAddressCommand(
                    owner.Organization.Id,
                    foreignAddressId,
                    "Stolen",
                    "Intruder",
                    "+639171234567",
                    "Unknown Street",
                    null,
                    null,
                    "Manila",
                    "Metro Manila",
                    "1000",
                    "PH",
                    true
                )
            )
        );
        await Should.ThrowAsync<KeyNotFoundException>(() =>
            SendAsync(new RemoveCustomerAddressCommand(owner.Organization.Id, foreignAddressId))
        );

        var foreignAddress = await FindAsync<CustomerAddress>(foreignAddressId);
        foreignAddress.ShouldNotBeNull();
        foreignAddress.IsActive.ShouldBeTrue();
    }

    private static async Task<Guid> AddAddressAsync(
        Guid organizationId,
        string label,
        bool makeDefault
    ) =>
        await SendAsync(
            new AddCustomerAddressCommand(
                organizationId,
                label,
                "Juan Dela Cruz",
                "+639171234567",
                "123 Test Street",
                null,
                "Test Barangay",
                "Manila",
                "Metro Manila",
                "1000",
                "PH",
                makeDefault
            )
        );

    private static async Task<CustomerTestData> CreateCustomerAsync(string suffix)
    {
        var organization = Organization.Create(
            $"Customer {suffix}",
            $"customer-{suffix}-{Guid.NewGuid():N}",
            "PHP"
        );
        var userId = await RunAsUserAsync(
            $"customer-{suffix}-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var customer = CustomerProfile.Create(organization.Id, userId, "Test Customer");
        await AddAsync(organization);
        await AddAsync(customer);
        return new CustomerTestData(organization, customer);
    }

    private sealed record CustomerTestData(Organization Organization, CustomerProfile Customer);
}
