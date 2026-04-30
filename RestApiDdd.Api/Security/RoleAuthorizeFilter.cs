using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RestApiDdd.Api.Security;

public sealed class RoleAuthorizeFilter(string[] roles) : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        if (roles.Length > 0 && !roles.Any(user.IsInRole))
        {
            context.Result = new ForbidResult();
        }

        return Task.CompletedTask;
    }
}
