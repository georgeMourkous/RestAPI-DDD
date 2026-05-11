using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using RestApiDdd.Service.Versioning;

namespace RestApiDdd.Api.Middleware;

public sealed class ApiVersionedDtoMiddleware(RequestDelegate next)
{
    private const string VersionRouteValueName = "version";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!TryGetRequestedVersion(context, out var requestedVersion))
        {
            await next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (endpoint is null || actionDescriptor is null)
        {
            await next(context);
            return;
        }

        var requestDtoType = GetRequestDtoType(actionDescriptor);
        if (requestDtoType is not null && IsJsonRequest(context.Request))
        {
            var unsupportedRequestProperties = await GetUnsupportedRequestPropertiesAsync(
                context.Request,
                requestDtoType,
                requestedVersion);

            if (unsupportedRequestProperties.Count > 0)
            {
                await WriteUnsupportedPropertyProblemAsync(context, requestedVersion, unsupportedRequestProperties);
                return;
            }
        }

        var responseDtoType = GetResponseDtoType(endpoint, actionDescriptor);
        if (responseDtoType is null)
        {
            await next(context);
            return;
        }

        var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await next(context);

            responseBuffer.Position = 0;
            if (!IsJsonResponse(context.Response) || responseBuffer.Length == 0)
            {
                await responseBuffer.CopyToAsync(originalResponseBody);
                return;
            }

            await using var filteredResponse = await FilterResponseAsync(responseBuffer, responseDtoType, requestedVersion);
            context.Response.ContentLength = filteredResponse.Length;
            filteredResponse.Position = 0;
            await filteredResponse.CopyToAsync(originalResponseBody);
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private static bool TryGetRequestedVersion(HttpContext context, out ApiVersion requestedVersion)
    {
        requestedVersion = default;

        if (!context.Request.RouteValues.TryGetValue(VersionRouteValueName, out var rawVersion)
            || rawVersion is null)
        {
            return false;
        }

        return Enum.TryParse(rawVersion.ToString(), ignoreCase: true, out requestedVersion)
            && Enum.IsDefined(requestedVersion);
    }

    private static Type? GetRequestDtoType(ControllerActionDescriptor actionDescriptor)
    {
        return actionDescriptor.Parameters
            .OfType<ControllerParameterDescriptor>()
            .Where(parameter => parameter.ParameterInfo.GetCustomAttribute<FromBodyAttribute>() is not null)
            .Select(parameter => parameter.ParameterType)
            .FirstOrDefault(IsDtoType);
    }

    private static Type? GetResponseDtoType(Endpoint endpoint, ControllerActionDescriptor actionDescriptor)
    {
        var responseType = endpoint.Metadata
            .OfType<ProducesResponseTypeAttribute>()
            .Where(attribute => attribute.StatusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices)
            .Select(attribute => attribute.Type)
            .FirstOrDefault(type => type is not null && type != typeof(void));

        return responseType ?? UnwrapActionResultType(actionDescriptor.MethodInfo.ReturnType);
    }

    private static Type? UnwrapActionResultType(Type returnType)
    {
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        if (returnType.IsGenericType)
        {
            return returnType.GetGenericArguments().FirstOrDefault(IsDtoOrDtoCollection);
        }

        return IsDtoOrDtoCollection(returnType) ? returnType : null;
    }

    private static async Task<IReadOnlyList<string>> GetUnsupportedRequestPropertiesAsync(
        HttpRequest request,
        Type dtoType,
        ApiVersion requestedVersion)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(request.Body);
        }
        catch (JsonException)
        {
            request.Body.Position = 0;
            return [];
        }

        request.Body.Position = 0;
        using (document)
        {
            var unsupportedProperties = new List<string>();
            CollectUnsupportedProperties(document.RootElement, dtoType, requestedVersion, unsupportedProperties, path: string.Empty);

            return unsupportedProperties;
        }
    }

    private static void CollectUnsupportedProperties(
        JsonElement element,
        Type targetType,
        ApiVersion requestedVersion,
        List<string> unsupportedProperties,
        string path)
    {
        var elementType = UnwrapCollectionType(targetType);
        if (element.ValueKind == JsonValueKind.Array && elementType is not null)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                CollectUnsupportedProperties(item, elementType, requestedVersion, unsupportedProperties, $"{path}[{index}]");
                index++;
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var properties = GetVersionedProperties(targetType);
        foreach (var jsonProperty in element.EnumerateObject())
        {
            if (!properties.TryGetValue(jsonProperty.Name, out var property))
            {
                continue;
            }

            var propertyPath = string.IsNullOrEmpty(path)
                ? jsonProperty.Name
                : $"{path}.{jsonProperty.Name}";

            if (!property.Supports(requestedVersion))
            {
                unsupportedProperties.Add(propertyPath);
                continue;
            }

            CollectUnsupportedProperties(jsonProperty.Value, property.PropertyType, requestedVersion, unsupportedProperties, propertyPath);
        }
    }

    private static async Task<MemoryStream> FilterResponseAsync(
        MemoryStream responseBuffer,
        Type dtoType,
        ApiVersion requestedVersion)
    {
        using var document = await JsonDocument.ParseAsync(responseBuffer);
        var filteredResponse = new MemoryStream();
        await using var writer = new Utf8JsonWriter(filteredResponse);

        WriteFilteredElement(document.RootElement, dtoType, requestedVersion, writer);
        await writer.FlushAsync();

        filteredResponse.Position = 0;
        return filteredResponse;
    }

    private static void WriteFilteredElement(
        JsonElement element,
        Type targetType,
        ApiVersion requestedVersion,
        Utf8JsonWriter writer)
    {
        var elementType = UnwrapCollectionType(targetType);
        if (element.ValueKind == JsonValueKind.Array && elementType is not null)
        {
            writer.WriteStartArray();
            foreach (var item in element.EnumerateArray())
            {
                WriteFilteredElement(item, elementType, requestedVersion, writer);
            }

            writer.WriteEndArray();
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            element.WriteTo(writer);
            return;
        }

        var properties = GetVersionedProperties(targetType);
        writer.WriteStartObject();
        foreach (var jsonProperty in element.EnumerateObject())
        {
            if (!properties.TryGetValue(jsonProperty.Name, out var property))
            {
                jsonProperty.WriteTo(writer);
                continue;
            }

            if (!property.Supports(requestedVersion))
            {
                continue;
            }

            writer.WritePropertyName(jsonProperty.Name);
            WriteFilteredElement(jsonProperty.Value, property.PropertyType, requestedVersion, writer);
        }

        writer.WriteEndObject();
    }

    private static Dictionary<string, VersionedProperty> GetVersionedProperties(Type dtoType)
    {
        var properties = new Dictionary<string, VersionedProperty>(StringComparer.OrdinalIgnoreCase);

        foreach (var propertyInfo in dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var jsonName = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(propertyInfo.Name);
            var property = new VersionedProperty(
                propertyInfo.PropertyType,
                propertyInfo.GetCustomAttribute<ApiSupportedAttribute>());

            properties[jsonName] = property;
            properties[propertyInfo.Name] = property;
        }

        return properties;
    }

    private static Type? UnwrapCollectionType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType
            && type.GetGenericTypeDefinition() is var genericType
            && (genericType == typeof(IEnumerable<>)
                || genericType == typeof(IReadOnlyList<>)
                || genericType == typeof(IReadOnlyCollection<>)
                || genericType == typeof(List<>)))
        {
            return type.GetGenericArguments()[0];
        }

        return type.GetInterfaces()
            .Where(interfaceType => interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(interfaceType => interfaceType.GetGenericArguments()[0])
            .FirstOrDefault();
    }

    private static bool IsJsonRequest(HttpRequest request)
    {
        return request.ContentLength is > 0
            && request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsJsonResponse(HttpResponse response)
    {
        return response.StatusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices
            && response.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsDtoOrDtoCollection(Type type)
    {
        return IsDtoType(type) || UnwrapCollectionType(type) is { } elementType && IsDtoType(elementType);
    }

    private static bool IsDtoType(Type type)
    {
        return type.Namespace?.StartsWith("RestApiDdd.Service.Dtos", StringComparison.Ordinal) == true;
    }

    private static async Task WriteUnsupportedPropertyProblemAsync(
        HttpContext context,
        ApiVersion requestedVersion,
        IReadOnlyList<string> unsupportedProperties)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Unsupported request body properties",
            Detail = $"The request body contains properties that are not supported by API version {requestedVersion}.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["properties"] = unsupportedProperties;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private sealed record VersionedProperty(Type PropertyType, ApiSupportedAttribute? Attribute)
    {
        public bool Supports(ApiVersion requestedVersion)
        {
            return Attribute?.Supports(requestedVersion) ?? true;
        }
    }
}
