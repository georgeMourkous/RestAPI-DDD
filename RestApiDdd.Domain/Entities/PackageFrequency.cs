using RestApiDdd.Domain.Common;

namespace RestApiDdd.Domain.Entities;

public sealed class PackageFrequency : Entity
{
    private PackageFrequency()
    {
    }

    private PackageFrequency(string name, int frequency, bool isActive, DateTime created)
    {
        Created = created;
        Update(name, frequency, isActive);
    }

    public string Name { get; private set; } = string.Empty;

    public int Frequency { get; private set; }

    public int PackageId { get; private set; }

    public Package? Package { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime Created { get; private set; }

    internal static PackageFrequency Create(string name, int frequency, bool isActive, DateTime created)
    {
        return new PackageFrequency(name, frequency, isActive, created);
    }

    internal void Update(string name, int frequency, bool isActive)
    {
        Name = Guard.RequiredMaxLength(name, nameof(Name), 255);
        if (frequency <= 0)
        {
            throw new DomainException("Frequency must be greater than zero.");
        }

        Frequency = frequency;
        IsActive = isActive;
    }
}
