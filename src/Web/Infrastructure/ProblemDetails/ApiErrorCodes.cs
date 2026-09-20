namespace modular_mlm.Web.Infrastructure;

public static class ApiErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string DomainRuleViolation = "domain_rule_violation";
    public const string UnsupportedMedia = "unsupported_media";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string IdempotencyConflict = "idempotency_conflict";
    public const string PlacementConflict = "placement_conflict";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string ProviderTimeout = "provider_timeout";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string RateLimitExceeded = "rate_limit_exceeded";
    public const string AntiforgeryValidationFailed = "antiforgery_validation_failed";
    public const string UnexpectedError = "unexpected_error";
}
