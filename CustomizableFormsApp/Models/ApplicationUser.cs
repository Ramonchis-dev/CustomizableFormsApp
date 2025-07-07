using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CustomizableFormsApp.Models
{
    // You can add additional profile data for users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        // Calculated property for full name
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}