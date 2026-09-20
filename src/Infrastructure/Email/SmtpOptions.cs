namespace modular_mlm.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Modular MLM";
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
