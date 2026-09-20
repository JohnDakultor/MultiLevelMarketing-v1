namespace modular_mlm.Application.Common.Interfaces;

public interface IAuditPayloadRedactor
{
    string? SerializeAndRedact(object? value);
    string? RedactJson(string? json);
    string? RedactText(string? value);
}
