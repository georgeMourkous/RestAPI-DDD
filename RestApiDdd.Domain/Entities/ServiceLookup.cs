using RestApiDdd.Domain.Common;

namespace RestApiDdd.Domain.Entities;

public sealed class ServiceLookup : Entity
{
    private ServiceLookup()
    {
    }

    public ServiceLookup(string name, string? description)
    {
        Update(name, description);
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public void Update(string name, string? description)
    {
        Name = Guard.RequiredMaxLength(name, nameof(Name), 255);
        Description = Guard.OptionalMaxLength(description, nameof(Description), 2000);
    }
}
