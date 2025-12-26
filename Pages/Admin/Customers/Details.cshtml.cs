using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin.Customers
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Client? Client { get; set; }
        public List<Commande> Commandes { get; set; } = new List<Commande>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Récupérer le client
            Client = await _context.Clients.FindAsync(id);

            if (Client == null)
            {
                return Page();
            }

            // Récupérer les commandes du client
            Commandes = await _context.Commandes
                .Where(c => c.ClientId == id)
                .OrderByDescending(c => c.DateCommande)
                .ToListAsync();

            return Page();
        }
    }
}