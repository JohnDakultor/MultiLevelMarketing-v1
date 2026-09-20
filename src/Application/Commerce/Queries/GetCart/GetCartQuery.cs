namespace modular_mlm.Application.Commerce.Queries.GetCart;

public sealed record GetCartQuery(Guid OrganizationId) : IRequest<CartDto>, ICustomerContextRequest;
