using Microsoft.AspNetCore.Mvc;

namespace RestApiDdd.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RoleAuthorizeAttribute : TypeFilterAttribute
{
    public RoleAuthorizeAttribute(params string[] roles)
        : base(typeof(RoleAuthorizeFilter))
    {
        Arguments = [roles];
    }
}
