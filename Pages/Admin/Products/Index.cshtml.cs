using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin.Products
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Produit> Products { get; set; } = new List<Produit>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StockFilter { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.Produits.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    query = query.Where(p =>
                        p.Nom.Contains(SearchTerm) ||
                        (p.Description != null && p.Description.Contains(SearchTerm)));
                }

                if (!string.IsNullOrWhiteSpace(StockFilter))
                {
                    switch (StockFilter)
                    {
                        case "enstock":
                            query = query.Where(p => p.Stock > 10);
                            break;
                        case "faible":
                            query = query.Where(p => p.Stock > 0 && p.Stock <= 10);
                            break;
                        case "rupture":
                            query = query.Where(p => p.Stock == 0);
                            break;
                    }
                }

                Products = await query.OrderByDescending(p => p.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
                Products = new List<Produit>();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var product = await _context.Produits.FindAsync(id);

                if (product == null)
                {
                    return NotFound();
                }

                _context.Produits.Remove(product);
                await _context.SaveChangesAsync();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
                return RedirectToPage();
            }
        }
    }
}