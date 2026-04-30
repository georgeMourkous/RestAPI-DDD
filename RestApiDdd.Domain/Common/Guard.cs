namespace RestApiDdd.Domain.Common;

internal static class Guard
{
    public static string RequiredMaxLength(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }

    public static string? OptionalMaxLength(string? value, string fieldName, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    public static int PositiveId(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException($"{fieldName} must be greater than zero.");
        }

        return value;
    }
}
