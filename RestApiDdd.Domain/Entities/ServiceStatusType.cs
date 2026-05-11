using RestApiDdd.Domain.Common;

namespace RestApiDdd.Domain.Entities;

public sealed class ServiceStatusType : Entity
{
    private ServiceStatusType()
    {
    }

    public ServiceStatusType(string name, int sortOrder, string tokenName)
    {
        Name = Guard.RequiredMaxLength(name, nameof(Name), 255);
        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.");
        }

        SortOrder = sortOrder;
        TokenName = Guard.RequiredMaxLength(tokenName, nameof(TokenName), 50);
    }

    public string Name { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public string TokenName { get; private set; } = string.Empty;
}
