using CustomizableFormsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Security.Claims; // Added for ClaimTypes
using System.Threading.Tasks;

namespace CustomizableFormsApp.Authorization
{
    public class TemplateOwnerHandler : AuthorizationHandler<OperationAuthorizationRequirement, Template>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Template resource)
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                return Task.CompletedTask;
            }

            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Compare the user's NameIdentifier claim (which is typically the UserId)
            // with the Template's AuthorId.
            if (resource.AuthorId == context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}