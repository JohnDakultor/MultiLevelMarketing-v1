namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class CommissionReleaseCursor
{
    private readonly object _sync = new();
    private int _offset;

    public int Read()
    {
        lock (_sync)
            return _offset;
    }

    public void Advance(int offset)
    {
        lock (_sync)
            _offset = Math.Max(0, offset);
    }
}
