using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Customers.Commands.ChangeCustomerStatus;
using modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails;
using modular_mlm.Application.Customers.Queries.GetAdminCustomers;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Customers;

using static Infrastructure.TestApp;

[NonParallelizable]
public sealed class AdminCustomerManagementTests : TestBase
{
    [Test]
    public async Task AdministratorCanSearchViewAndChangeValidCustomerStatus()
    {
        var organization = Organization.Create("Customers", $"customers-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var customer = CustomerProfile.Create(
            organization.Id,
            "customer-user",
            "Maria Santos",
            "maria@example.test"
        );
        await AddAsync(customer);
        await RunAsAdministratorAsync(organization.Id);

        var page = await SendAsync(
            new GetAdminCustomersQuery(organization.Id, Search: "MARIA@EXAMPLE")
        );
        page.TotalCount.ShouldBe(1);
        page.Items.Single().Id.ShouldBe(customer.Id);
        (await SendAsync(new GetAdminCustomerDetailsQuery(organization.Id, customer.Id)))
            .ShouldNotBeNull();

        await SendAsync(
            new ChangeCustomerStatusCommand(
                organization.Id,
                customer.Id,
                CustomerStatus.Suspended,
                "Account review"
            )
        );
        (await FindAsync<CustomerProfile>(customer.Id))!.Status.ShouldBe(CustomerStatus.Suspended);
        await SendAsync(
            new ChangeCustomerStatusCommand(
                organization.Id,
                customer.Id,
                CustomerStatus.Active,
                "Review completed"
            )
        );
        (await FindAsync<CustomerProfile>(customer.Id))!.Status.ShouldBe(CustomerStatus.Active);
    }

    [Test]
    public async Task AdministratorCannotManageAnotherTenantsCustomerAndDisabledIsTerminal()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owned);
        await AddAsync(foreign);
        var customer = CustomerProfile.Create(foreign.Id, "foreign-user", "Foreign Customer");
        var ownedCustomer = CustomerProfile.Create(owned.Id, "owned-user", "Owned Customer");
        await AddAsync(customer);
        await AddAsync(ownedCustomer);
        await RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAdminCustomerDetailsQuery(foreign.Id, customer.Id))
        );

        await SendAsync(
            new ChangeCustomerStatusCommand(
                owned.Id,
                ownedCustomer.Id,
                CustomerStatus.Disabled,
                "Requested closure"
            )
        );
        await Should.ThrowAsync<modular_mlm.Domain.Exceptions.DomainInvariantException>(() =>
            SendAsync(
                new ChangeCustomerStatusCommand(
                    owned.Id,
                    ownedCustomer.Id,
                    CustomerStatus.Active,
                    "Invalid reactivation"
                )
            )
        );
    }
}
