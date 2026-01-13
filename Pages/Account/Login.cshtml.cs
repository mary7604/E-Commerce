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

        // ⭐ Pour savoir où rediriger après connexion
        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? TempData["ReturnUrl"]?.ToString();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? TempData["ReturnUrl"]?.ToString();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = await _authService.Login(Email, Password);

            if (client == null)
            {
                TempData["Error"] = " Email ou mot de passe incorrect";
                return Page();
            }

            //  Stocker les infos client en session
            HttpContext.Session.SetInt32("ClientId", client.Id);
            HttpContext.Session.SetString("ClientName", $"{client.Prenom} {client.Nom}");
            HttpContext.Session.SetString("ClientEmail", client.Email);

            if (client.EstAdmin)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
            }

            TempData["Message"] = $" Bienvenue {client.Prenom} !";

            // Rediriger vers la page demandée (Checkout) ou Index
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                Console.WriteLine($"Redirection vers : {ReturnUrl}");
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }
    }
}
