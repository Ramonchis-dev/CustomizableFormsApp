using System.Security.Claims;
using CustomizableFormsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomizableFormsApp.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly CustomizableFormsAppDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CustomizableFormsAppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    public async Task<IdentityResult> RegisterAsync(string email, string password)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        return result;
    }

    public async Task<SignInResult> LoginAsync(string email, string password, bool rememberMe)
    {
        return await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<ApplicationUser> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        return await _userManager.GetUserAsync(principal);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _userManager.Users
            .Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                Email = u.Email,
                Roles = _userManager.GetRolesAsync(u).Result.ToList(),
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    public async Task<IdentityResult> UpdateProfileAsync(string userId, string fullName, string phoneNumber)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed();

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;

        return await _userManager.UpdateAsync(user);
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
    }
}