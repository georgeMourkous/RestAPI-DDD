namespace RestApiDdd.Service.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
