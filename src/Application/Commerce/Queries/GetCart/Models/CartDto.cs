public sealed record CartDto(
    Guid? Id,
    Guid OrganizationId,
    Guid? CustomerId,
    bool HasAnonymousSession,
    Guid? AttributedAgentId,
    string? ReferralCode,
    IReadOnlyList<CartItemDto> Items,
    string Currency,
    decimal Subtotal,
    int ItemCount
);
