using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Nom { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string? Telephone { get; set; }

        [BindProperty]
        public string? Adresse { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
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
                // Vérifier si l'email existe déjà
                var existingClient = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Email == Email);

                if (existingClient != null)
                {
                    ErrorMessage = "Un compte avec cet email existe déjà. Veuillez vous connecter.";
                    return Page();
                }

                // Créer le nouveau client
                var newClient = new Client
                {
                    Nom = Nom,
                    Email = Email,
                    Telephone = Telephone,
                    Adresse = Adresse
                };

                _context.Clients.Add(newClient);
                await _context.SaveChangesAsync();

                // Connecter automatiquement le client
                HttpContext.Session.SetString("IsClientLoggedIn", "true");
                HttpContext.Session.SetString("ClientId", newClient.Id.ToString());
                HttpContext.Session.SetString("ClientNom", newClient.Nom);
                HttpContext.Session.SetString("ClientEmail", newClient.Email);

                // Rediriger vers la page d'origine ou le profil
                var returnUrl = Request.Query["returnUrl"].ToString();
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToPage("/Account/Profile");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la création du compte : {ex.Message}";
                return Page();
            }
        }
    }
}
