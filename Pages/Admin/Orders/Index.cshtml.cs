using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin.Orders
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Commande> Commandes { get; set; } = new List<Commande>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatutFilter { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.Commandes
                    .Include(c => c.Client)
                    .AsQueryable();

                // Filtre de recherche
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    query = query.Where(c =>
                        c.Id.ToString().Contains(SearchTerm) ||
                        (c.Client != null && c.Client.Nom.Contains(SearchTerm)));
                }

                // Filtre par statut
                if (!string.IsNullOrWhiteSpace(StatutFilter))
                {
                    query = query.Where(c => c.Statut == StatutFilter);
                }

                Commandes = await query
                    .OrderByDescending(c => c.DateCommande)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
                Commandes = new List<Commande>();
            }
        }
    }
}