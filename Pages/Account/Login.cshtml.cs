using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ClientAuthService _authService;

        public LoginModel(ClientAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = await _authService.Login(Email, Password);

            if (client == null)
            {
                TempData["Error"] = "Email ou mot de passe incorrect";
                return Page();
            }

            // Stocker les infos client en session
            HttpContext.Session.SetInt32("ClientId", client.Id);
            HttpContext.Session.SetString("ClientName", $"{client.Prenom} {client.Nom}");
            HttpContext.Session.SetString("ClientEmail", client.Email);

            if (client.EstAdmin)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
            }

            TempData["Message"] = $"Bienvenue {client.Prenom} !";
            return RedirectToPage("/Index");
        }
    }
}
