using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet(string? message)
        {
            if (message == "registered")
            {
                SuccessMessage = "Compte créé avec succès ! Connectez-vous maintenant.";
            }

            // Si déjà connecté, rediriger vers le profil
            if (HttpContext.Session.GetString("IsClientLoggedIn") == "true")
            {
                Response.Redirect("/Account/Profile");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Rechercher le client dans la base de données
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Email == Email);

                // Vérifier email et mot de passe
                if (client != null && client.Password == Password)
                {
                    // Créer une session client avec protection NULL
                    HttpContext.Session.SetString("IsClientLoggedIn", "true");
                    HttpContext.Session.SetString("ClientId", client.Id.ToString());
                    HttpContext.Session.SetString("ClientNom", client.Nom ?? "Client");
                    HttpContext.Session.SetString("ClientEmail", client.Email ?? "");  

                    // Rediriger vers la page d'origine ou la boutique
                    var returnUrl = Request.Query["returnUrl"].ToString();
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToPage("/Index");
                }

                ErrorMessage = "Email ou mot de passe incorrect.";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur : {ex.Message}";
                return Page();
            }
        }
    }
}