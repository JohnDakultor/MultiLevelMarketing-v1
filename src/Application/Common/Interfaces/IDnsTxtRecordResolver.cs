namespace modular_mlm.Application.Common.Interfaces;

public interface IDnsTxtRecordResolver
{
    Task<bool> ContainsAsync(
        string recordName,
        string expectedValue,
        CancellationToken cancellationToken
    );
}
