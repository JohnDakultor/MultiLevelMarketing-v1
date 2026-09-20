namespace modular_mlm.Web.Contracts.Messaging;

public sealed record ReplayDeadLetterMessageRequest(int ExpectedAttempts, string Reason);
