namespace modular_mlm.Web.Contracts.Organizations;

public sealed record ConfigureOrganizationDomainRequest(string HostName, bool MakePrimary);
