using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin.Orders
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Commande? Commande { get; set; }
        public Client? Client { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Récupérer la commande
            Commande = await _context.Commandes.FindAsync(id);

            if (Commande == null)
            {
                return Page();
            }

            // Récupérer le client
            Client = await _context.Clients.FindAsync(Commande.ClientId);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
        {
            var commande = await _context.Commandes.FindAsync(id);

            if (commande != null)
            {
                commande.Statut = newStatus;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }
    }
}