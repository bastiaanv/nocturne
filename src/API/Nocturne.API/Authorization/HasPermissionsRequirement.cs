using Microsoft.AspNetCore.Authorization;
using Nocturne.Core.Models;

namespace Nocturne.API.Authorization;

/// <summary>
/// Authorization requirement that succeeds when the request has any granted permissions
/// in the PermissionTrie, regardless of whether the user is authenticated.
/// </summary>
public class HasPermissionsRequirement : IAuthorizationRequirement;

public static class PolicyNames
{
    public const string HasPermissions = "HasPermissions";
}

/// <summary>
/// Handles <see cref="HasPermissionsRequirement"/> by checking for a non-empty
/// PermissionTrie in HttpContext.Items.
/// </summary>
public class HasPermissionsHandler : AuthorizationHandler<HasPermissionsRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HasPermissionsHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionsRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.Items["PermissionTrie"] is PermissionTrie trie && !trie.IsEmpty)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
