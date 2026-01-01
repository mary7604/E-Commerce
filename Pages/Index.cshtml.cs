using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public IndexModel(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public List<Produit> Produits { get; set; } = new List<Produit>();
        public string? SearchTerm { get; set; }
        public int CartCount { get; set; }

        public async Task OnGetAsync(string? search)
        {
            SearchTerm = search;

            // Si recherche pas du cache
            if (!string.IsNullOrWhiteSpace(search))
            {
                var query = _context.Produits
                    .Where(p => p.Nom.Contains(search) ||
                               (p.Description != null && p.Description.Contains(search)));

                Produits = await query
                    .OrderByDescending(p => p.Stock)
                    .ToListAsync();
            }
            else
            {
                // Pas de recherche == UTILISER LE CACHE
                Produits = await GetProduitsFromCache();
            }

            // Calculer le nombre d'articles dans le panier
            CartCount = GetCartCount();
        }

        //  NOUVELLE MÉTHODE AVEC CACHE
        private async Task<List<Produit>> GetProduitsFromCache()
        {
            const string cacheKey = "produits_liste";

            // Essayer de lire depuis le cache
            if (!_cache.TryGetValue(cacheKey, out List<Produit> produits))
            {
                // Cache MISS alors Requête BDD
                Console.WriteLine(" CACHE MISS - Requête BDD");

                produits = await _context.Produits
                    .OrderByDescending(p => p.Stock)
                    .ToListAsync();

                // Mettre en cache pour 5 min
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, produits, cacheOptions);
            }
            else
            {
                // Lecture cache
                Console.WriteLine("CACHE HIT - Depuis cache");
            }

            return produits;
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int id)
        {
            var produit = await _context.Produits.FindAsync(id);

            if (produit == null || produit.Stock == 0)
            {
                return RedirectToPage();
            }

            var cart = GetCartFromSessionOrCookie();

            var existingItem = cart.FirstOrDefault(item => item.ProduitId == id);

            if (existingItem != null)
            {
                if (existingItem.Quantite < produit.Stock)
                {
                    existingItem.Quantite++;
                }
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProduitId = produit.Id,
                    Nom = produit.Nom,
                    Prix = produit.Prix,
                    Quantite = 1,
                    ImageUrl = produit.ImageUrl
                });
            }

            SaveCartToSessionAndCookie(cart);

            TempData["Message"] = "Produit ajouté au panier !";
            return RedirectToPage();
        }

        private List<CartItem> GetCartFromSessionOrCookie()
        {
            // Essayer de lire depuis la session
            var cartJson = HttpContext.Session.GetString("Cart");

            // Si pas dans session, essayer depuis les cookies
            if (string.IsNullOrEmpty(cartJson))
            {
                cartJson = Request.Cookies["Cart"];
            }

            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SaveCartToSessionAndCookie(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);

            // Sauvegarder dans la session
            HttpContext.Session.SetString("Cart", cartJson);

            // Sauvegarder aussi dans un cookie persistant (30 jrs)
            Response.Cookies.Append("Cart", cartJson, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }

        private int GetCartCount()
        {
            var cart = GetCartFromSessionOrCookie();
            return cart.Sum(item => item.Quantite);
        }
    }

    // Classe CartItem 
    public class CartItem
    {
        public int ProduitId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public int Quantite { get; set; }
        public string? ImageUrl { get; set; }
    }
}
