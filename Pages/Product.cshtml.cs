using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class ProductModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProductModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Produit? Produit { get; set; }
        public List<Avis> Avis { get; set; } = new List<Avis>();
        public double NoteMoyenne { get; set; }
        public int NombreAvis { get; set; }
        public bool IsClientLoggedIn { get; set; }
        public bool ClientADejaCommente { get; set; }

        [BindProperty]
        public int Note { get; set; }

        [BindProperty]
        public string? Commentaire { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Produit = await _context.Produits.FindAsync(id);
            if (Produit == null) return Page();

            // ✅ CHARGER TOUS LES AVIS (pas seulement approuvés)
            Avis = await _context.Avis
                .Include(a => a.Client)
                .Where(a => a.ProduitId == id) // Supprimé && a.EstApprouve
                .OrderByDescending(a => a.DateAvis)
                .ToListAsync();

            if (Avis.Any())
            {
                NoteMoyenne = Avis.Average(a => a.Note);
                NombreAvis = Avis.Count;
            }

            var clientIdStr = HttpContext.Session.GetString("ClientId");
            IsClientLoggedIn = !string.IsNullOrEmpty(clientIdStr);

            if (IsClientLoggedIn)
            {
                var clientId = int.Parse(clientIdStr);
                ClientADejaCommente = await _context.Avis
                    .AnyAsync(a => a.ProduitId == id && a.ClientId == clientId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var clientIdStr = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientIdStr))
            {
                return RedirectToPage("/Account/Login", new { returnUrl = $"/Product/{id}" });
            }

            var clientId = int.Parse(clientIdStr);

            var dejaCommente = await _context.Avis
                .AnyAsync(a => a.ProduitId == id && a.ClientId == clientId);

            if (dejaCommente)
            {
                return RedirectToPage(new { id });
            }

            // ✅ CRÉER L'AVIS DIRECTEMENT APPROUVÉ
            var avis = new Avis
            {
                ProduitId = id,
                ClientId = clientId,
                Note = Note,
                Commentaire = Commentaire,
                DateAvis = DateTime.Now,
                EstApprouve = true  // ✅ Publié immédiatement
            };

            _context.Avis.Add(avis);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }
    }
}