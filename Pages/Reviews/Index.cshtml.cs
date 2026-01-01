using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

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

            //  STATISTIQUES GLOBALES - CORRIGÉ
            var totalAvis = await _context.Avis.CountAsync();

            if (totalAvis > 0)
            {
                // Il y a des avis, on peut calculer les stats
                TotalAvis = totalAvis;
                AvisEnAttente = await _context.Avis.CountAsync(a => !a.EstApprouve);
                AvisApprouves = await _context.Avis.CountAsync(a => a.EstApprouve);
                NoteMoyenneGlobale = await _context.Avis.AverageAsync(a => (double)a.Note);
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

            TempData["Message"] = "Avis approuvé avec succès !";
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

            TempData["Message"] = "Avis supprimé avec succès !";
            return RedirectToPage();
        }
    }
}