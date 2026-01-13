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
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;

        //  Pour savoir où rediriger après inscription
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

            var client = await _authService.Register(Nom, Prenom, Email, Password, Telephone, Adresse);

            if (client == null)
            {
                TempData["Error"] = " Cet email est déjà utilisé";
                return Page();
            }

            //  Connecter automatiquement après inscription
            HttpContext.Session.SetInt32("ClientId", client.Id);
            HttpContext.Session.SetString("ClientName", $"{client.Prenom} {client.Nom}");
            HttpContext.Session.SetString("ClientEmail", client.Email);

            TempData["Message"] = " Compte créé avec succès ! Bienvenue !";

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
