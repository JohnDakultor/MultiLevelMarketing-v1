using System.Text.Json;

namespace modular_mlm.Application.Common.Auditing;

public static class AuditJson
{
    public static string Serialize(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value);
    }
}
