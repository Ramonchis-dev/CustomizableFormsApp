using Microsoft.AspNetCore.Authorization;

namespace CustomizableFormsApp.Authorization;

public class TemplateOwnerRequirement : IAuthorizationRequirement
{
    // This is a marker requirement with no properties
    // The actual ownership check is done in the handler
}