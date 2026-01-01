using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ClientAuthService _authService;

        public RegisterModel(ClientAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        [Required]
        public string Prenom { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string Nom { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string? Telephone { get; set; }

        [BindProperty]
        public string? Adresse { get; set; }

        [BindProperty]
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = await _authService.Register(Nom, Prenom, Email, Password, Telephone, Adresse);

            if (client == null)
            {
                TempData["Error"] = "Cet email est déjà utilisé";
                return Page();
            }

            // Connecter automatiquement après inscription
            HttpContext.Session.SetInt32("ClientId", client.Id);
            HttpContext.Session.SetString("ClientName", $"{client.Prenom} {client.Nom}");
            HttpContext.Session.SetString("ClientEmail", client.Email);

            TempData["Message"] = "Compte créé avec succès !";
            return RedirectToPage("/Index");
        }
    }
}
