using RestApiDdd.Domain.Common;

namespace RestApiDdd.Domain.Entities;

public sealed class PackageCategory : Entity
{
    private PackageCategory()
    {
    }

    public PackageCategory(string name, int sortOrder, bool visible)
    {
        Update(name, sortOrder, visible);
    }

    public string Name { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool Visible { get; private set; }

    public void Update(string name, int sortOrder, bool visible)
    {
        Name = Guard.RequiredMaxLength(name, nameof(Name), 255);
        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.");
        }

        SortOrder = sortOrder;
        Visible = visible;
    }
}
