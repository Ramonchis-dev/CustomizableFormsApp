using CustomizableFormsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CustomizableFormsApp.Authorization;

public class TemplateOwnerHandler : AuthorizationHandler<TemplateOwnerRequirement, Template>
{
    private readonly CustomizableFormsAppDbContext _context;

    public TemplateOwnerHandler(CustomizableFormsAppDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TemplateOwnerRequirement requirement,
        Template resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var template = await _context.Templates
                .FirstOrDefaultAsync(t => t.Id == resource.Id && t.AuthorId == userGuid);

            if (template != null)
            {
                context.Succeed(requirement);
            }
        }
    }
}