namespace RestApiDdd.Service.Abstractions;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
