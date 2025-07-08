using CustomizableFormsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // For List
using System.Linq; // For LINQ operations
using System.Security.Claims;
using System.Threading.Tasks;

namespace CustomizableFormsApp.Services
{
    public class AuthService(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) // Inject RoleManager too
    {
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager; // Store RoleManager

        public async Task<SignInResult> PasswordSignInAsync(string email, string password, bool rememberMe)
        {
            return await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
        }

        public async Task<IdentityResult> RegisterUserAsync(string email, string password, string firstName, string lastName)
        {
            var user = new ApplicationUser { UserName = email, Email = email, FirstName = firstName, LastName = lastName };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Assign "User" role by default to new registrations
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
            }

            return result;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        // --- NEW METHODS ---

        public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
        {
            return await _userManager.GetUserAsync(userPrincipal);
        }

        public async Task<IdentityResult> UpdateProfileAsync(ApplicationUser user)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.UserName = user.Email; // Keep UserName and Email in sync for Identity

            return await _userManager.UpdateAsync(existingUser);
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> UpdateUserRolesAsync(ApplicationUser user, IEnumerable<string> newRoles)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = newRoles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(newRoles);

            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded) return addResult;

            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            return removeResult;
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }
            return await _userManager.DeleteAsync(user);
        }

    }
}