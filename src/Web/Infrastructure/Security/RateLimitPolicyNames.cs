namespace modular_mlm.Web.Infrastructure.Security;

public static class RateLimitPolicyNames
{
    public const string Financial = "financial";
    public const string Authentication = "authentication";
    public const string Invitation = "invitation";
    public const string Checkout = "checkout";
    public const string Payment = "payment";
    public const string Payout = "payout";
    public const string Refund = "refund";
    public const string AdministratorFinancialAction = "administrator-financial";
    public const string Webhook = "webhook";
}
