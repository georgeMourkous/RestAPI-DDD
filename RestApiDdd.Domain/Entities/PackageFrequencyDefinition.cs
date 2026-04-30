namespace RestApiDdd.Domain.Entities;

public sealed record PackageFrequencyDefinition(
    int Id,
    string Name,
    int Frequency,
    bool IsActive);
