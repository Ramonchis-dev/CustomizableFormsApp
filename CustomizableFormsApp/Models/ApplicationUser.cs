using Microsoft.AspNetCore.Identity;

namespace CustomizableFormsApp.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
}