using DnsClient;
using DnsClient.Protocol;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Organizations;

public sealed class DnsTxtRecordResolver(LookupClient client) : IDnsTxtRecordResolver
{
    public async Task<bool> ContainsAsync(
        string recordName,
        string expectedValue,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await client.QueryAsync(
                recordName,
                QueryType.TXT,
                QueryClass.IN,
                cancellationToken
            );
            return result
                .Answers.TxtRecords()
                .SelectMany(record => record.Text)
                .Any(value => string.Equals(value.Trim(), expectedValue, StringComparison.Ordinal));
        }
        catch (DnsResponseException)
        {
            return false;
        }
    }
}
