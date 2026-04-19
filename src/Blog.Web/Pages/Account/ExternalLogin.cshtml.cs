using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Blog.Domain.Entities;
using System.Security.Claims;

namespace Blog.Web.Pages.Account;

[IgnoreAntiforgeryToken]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ProviderDisplayName { get; set; }
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
    }

    public IActionResult OnGet() => RedirectToPage("./Login");

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        // If the user does not have an account, create one
        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Email claim not received from external provider.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Check if user already exists with this email
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            // Link external login to existing user
            var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            ErrorMessage = "Failed to link external login to existing account.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Create new user
        var userName = await GenerateUserNameAsync(name ?? email.Split('@')[0]);
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true, // Email is verified by OAuth provider
            DisplayName = name ?? userName,
            CreatedAt = DateTime.UtcNow
        };

        // Get avatar URL from OAuth provider
        var avatarUrl = GetAvatarUrl(info);
        if (!string.IsNullOrEmpty(avatarUrl))
        {
            user.AvatarUrl = avatarUrl;
        }

        var createResult = await _userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            createResult = await _userManager.AddLoginAsync(user, info);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Author");
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
        }

        foreach (var error in createResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ErrorMessage = "Failed to create account. Please try again.";
        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
    }

    private async Task<string> GenerateUserNameAsync(string baseName)
    {
        var userName = new string(baseName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray()).ToLower();
        if (string.IsNullOrEmpty(userName) || userName.Length < 3)
            userName = "user" + Random.Shared.Next(10000, 99999);

        var candidate = userName;
        var suffix = 1;
        while (await _userManager.FindByNameAsync(candidate) != null)
        {
            candidate = $"{userName}{suffix}";
            suffix++;
        }
        return candidate;
    }

    private static string? GetAvatarUrl(ExternalLoginInfo info)
    {
        // Try to get avatar from different providers
        var picture = info.Principal.FindFirstValue("picture"); // Google
        if (!string.IsNullOrEmpty(picture))
            return picture;

        // GitHub avatar
        var avatarUrl = info.Principal.FindFirstValue("urn:github:avatar");
        if (!string.IsNullOrEmpty(avatarUrl))
            return avatarUrl;

        return null;
    }
}
