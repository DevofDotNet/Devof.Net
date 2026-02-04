using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Blog.Web.Pages;

public class ContactModel : PageModel
{
    private readonly ILogger<ContactModel> _logger;

    public ContactModel(ILogger<ContactModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Log the contact form submission
        _logger.LogInformation("Contact form submitted: {Name}, {Email}, {Subject}",
            Input.Name, Input.Email, Input.Subject);

        // In a real application, you would send an email here
        // For now, just show a success message
        TempData["Success"] = "Thank you for your message! We'll get back to you soon.";

        return RedirectToPage();
    }
}
