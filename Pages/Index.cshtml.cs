using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly CartService _cartService;

        public IndexModel(ApplicationDbContext context, IMemoryCache cache, CartService cartService)
        {
            _context = context;
            _cache = cache;
            _cartService = cartService;
        }

        public List<Produit> Produits { get; set; } = new List<Produit>();
        public List<string> Categories { get; set; } = new List<string>();

        public string? SearchTerm { get; set; }
        public List<string>? SelectedCategories { get; set; }
        public decimal? MinPrix { get; set; }
        public decimal? MaxPrix { get; set; }

        public int CartCount { get; set; }

        public async Task OnGetAsync(
            string? search,
            List<string>? categories = null,
            decimal? minPrix = null,
            decimal? maxPrix = null)
        {
            SearchTerm = search;
            SelectedCategories = categories;
            MinPrix = minPrix;
            MaxPrix = maxPrix;

            Categories = await GetCategoriesFromCache();

            bool hasFilters = !string.IsNullOrWhiteSpace(search) ||
                              (categories != null && categories.Any()) ||
                              minPrix.HasValue ||
                              maxPrix.HasValue;

            if (hasFilters)
            {
                Produits = await GetFilteredProduitsAsync(search, categories, minPrix, maxPrix);
            }
            else
            {
                Produits = await GetProduitsFromCache();
            }

            CartCount = await GetCartCountAsync();
        }

        private async Task<List<Produit>> GetProduitsFromCache()
        {
            const string cacheKey = "produits_liste";

            if (!_cache.TryGetValue(cacheKey, out List<Produit> produits))
            {
                produits = await _context.Produits
                    .AsNoTracking()
                    .Where(p => p.Stock > 0)
                    .OrderByDescending(p => p.DateAjout)
                    .ToListAsync();

                _cache.Set(cacheKey, produits, TimeSpan.FromMinutes(5));
            }

            return produits;
        }

        private async Task<List<Produit>> GetFilteredProduitsAsync(
            string? search, List<string>? categories, decimal? minPrix, decimal? maxPrix)
        {
            var query = _context.Produits.AsNoTracking();
          

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Nom.Contains(search) || (p.Description != null && p.Description.Contains(search)));

            if (categories != null && categories.Any())
                query = query.Where(p => !string.IsNullOrEmpty(p.Categorie) && categories.Contains(p.Categorie));

            if (minPrix.HasValue && minPrix.Value > 0)
                query = query.Where(p => p.Prix >= minPrix.Value);

            if (maxPrix.HasValue && maxPrix.Value < 5000)
                query = query.Where(p => p.Prix <= maxPrix.Value);

            return await query.OrderByDescending(p => p.DateAjout).ToListAsync();
        }

        private async Task<List<string>> GetCategoriesFromCache()
        {
            const string cacheKey = "categories_liste";

            if (!_cache.TryGetValue(cacheKey, out List<string> categories))
            {
                categories = await _context.Produits
                     .AsNoTracking()
                    .Where(p => !string.IsNullOrEmpty(p.Categorie))
                    .Select(p => p.Categorie!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(10));
            }

            return categories;
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int id)
        {
            var produit = await _context.Produits.FindAsync(id);

            if (produit == null)
            {
                TempData["Error"] = " Produit introuvable";
                return RedirectToPage();
            }

            if (produit.Stock == 0)
            {
                TempData["Error"] = $" '{produit.Nom}' est en rupture de stock";
                return RedirectToPage();
            }

            string userId = GetUserId();

            var item = new CartItem
            {
                ProduitId = produit.Id,
                Nom = produit.Nom,
                Prix = produit.Prix,
                Quantite = 1,
                ImageUrl = produit.ImageUrl
            };

            var result = await _cartService.AddToCartAsync(userId, item);

            if (result.Success)
                TempData["Message"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToPage();
        }

        private async Task<int> GetCartCountAsync()
        {
            return await _cartService.GetCartCountAsync(GetUserId());
        }

        private string GetUserId()
        {
            //  Utilisateur connecté
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.Identity.Name ?? "guest";
            }

            // Visiteur non connecté = Cookie permanent (30 jours)
            string? userId = Request.Cookies["GuestCartId"];

            if (string.IsNullOrEmpty(userId))
            {
                // Créer un nouvel ID unique
                userId = $"guest_{Guid.NewGuid()}";

                // Stocker dans un cookie permanent
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true
                };

                Response.Cookies.Append("GuestCartId", userId, cookieOptions);
            }

            return userId;
        }
        public async Task<IActionResult> OnGetCartCountAsync()
        {
            var count = await GetCartCountAsync();
            return new JsonResult(new { count });
        }
    }
}