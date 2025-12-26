using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using AdminModel = WebApplication1.Models.Admin;

namespace WebApplication1.Pages.Reviews
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Avis> Avis { get; set; } = new List<Avis>();
        public int AvisEnAttente { get; set; }
        public int AvisApprouves { get; set; }
        public int TotalAvis { get; set; }
        public double NoteMoyenneGlobale { get; set; }
        public string? Statut { get; set; }

        public async Task OnGetAsync(string? statut, int? note)
        {
            Statut = statut;

            // Query pour afficher les avis filtrés
            var query = _context.Avis
                .Include(a => a.Produit)
                .Include(a => a.Client)
                .AsQueryable();

            // Filtrer par statut
            if (statut == "attente")
            {
                query = query.Where(a => !a.EstApprouve);
            }
            else if (statut == "approuve")
            {
                query = query.Where(a => a.EstApprouve);
            }

            // Filtrer par note
            if (note.HasValue)
            {
                query = query.Where(a => a.Note == note.Value);
            }

            // Récupérer les avis filtrés
            Avis = await query
                .OrderByDescending(a => a.DateAvis)
                .ToListAsync();

            // Statistiques globales (optimisé)
            var statistiques = await _context.Avis
                .GroupBy(a => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    EnAttente = g.Count(a => !a.EstApprouve),
                    Approuves = g.Count(a => a.EstApprouve),
                    MoyenneNote = g.Average(a => (double)a.Note)
                })
                .FirstOrDefaultAsync();

            if (statistiques != null)
            {
                TotalAvis = statistiques.Total;
                AvisEnAttente = statistiques.EnAttente;
                AvisApprouves = statistiques.Approuves;
                NoteMoyenneGlobale = statistiques.MoyenneNote;
            }
            else
            {
                // Aucun avis dans la base
                TotalAvis = 0;
                AvisEnAttente = 0;
                AvisApprouves = 0;
                NoteMoyenneGlobale = 0;
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var avis = await _context.Avis.FindAsync(id);
            
            if (avis == null)
            {
                return NotFound();
            }

            avis.EstApprouve = true;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var avis = await _context.Avis.FindAsync(id);
            
            if (avis == null)
            {
                return NotFound();
            }

            _context.Avis.Remove(avis);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}