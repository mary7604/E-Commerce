using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Client? Client { get; set; }
        public List<Commande> Commandes { get; set; } = new List<Commande>();

        public async Task<IActionResult> OnGetAsync()
        {
            //  Lire ClientId  
            int? clientId = HttpContext.Session.GetInt32("ClientId");

            if (!clientId.HasValue)
            {
                // Utilisateur non connecté
                return RedirectToPage("/Account/Login");
            }

            // Charger les informations du client
            Client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == clientId.Value);

            if (Client == null)
            {
                // Session invalide
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            // Charger l'historique des commandes
            Commandes = await _context.Commandes
                .Where(c => c.ClientId == clientId.Value)
                .OrderByDescending(c => c.DateCommande)
                .ToListAsync();

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            // Effacer la session
            HttpContext.Session.Clear();

            // Rediriger vers la page d'accueil
            return RedirectToPage("/Index");
        }
    }
}
