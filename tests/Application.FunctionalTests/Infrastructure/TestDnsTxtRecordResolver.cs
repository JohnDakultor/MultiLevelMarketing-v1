using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.FunctionalTests.Infrastructure;

public sealed class TestDnsTxtRecordResolver : IDnsTxtRecordResolver
{
    private readonly Dictionary<string, HashSet<string>> _records =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> ContainsAsync(
        string recordName,
        string expectedValue,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            _records.TryGetValue(recordName, out var values) && values.Contains(expectedValue)
        );

    public void Add(string recordName, string value)
    {
        if (!_records.TryGetValue(recordName, out var values))
        {
            values = new HashSet<string>(StringComparer.Ordinal);
            _records.Add(recordName, values);
        }
        values.Add(value);
    }

    public void Reset() => _records.Clear();
}
