using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApplication1.Data;

namespace WebApplication1.Pages
{
    public class CartModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CartModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public int TotalItems { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCart();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int id)
        {
            var cart = GetCartFromSession();
            cart.RemoveAll(item => item.ProduitId == id);
            SaveCartToSession(cart);

            TempData["Message"] = "Produit retiré du panier";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync(int id, string action)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProduitId == id);

            if (item != null)
            {
                var produit = await _context.Produits.FindAsync(id);
                if (produit != null)
                {
                    if (action == "increase" && item.Quantite < produit.Stock)
                    {
                        item.Quantite++;
                    }
                    else if (action == "decrease")
                    {
                        item.Quantite--;
                        if (item.Quantite <= 0)
                        {
                            cart.Remove(item);
                        }
                    }

                    SaveCartToSession(cart);
                    TempData["Message"] = "Quantité mise à jour";
                }
            }

            return RedirectToPage();
        }

        private async Task LoadCart()
        {
            var cart = GetCartFromSession();

            var produitIds = cart.Select(c => c.ProduitId).ToList();
            var produits = await _context.Produits
                .Where(p => produitIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            CartItems = cart.Select(c => {
                if (produits.TryGetValue(c.ProduitId, out var produit))
                {
                    c.Nom = produit.Nom;
                    c.Prix = produit.Prix;
                    c.ImageUrl = produit.ImageUrl;
                    if (c.Quantite > produit.Stock)
                    {
                        c.Quantite = produit.Stock;
                    }
                }
                return c;
            }).ToList();

            Subtotal = CartItems.Sum(item => item.Prix * item.Quantite);
            ShippingCost = Subtotal >= 500 ? 0 : 50;
            Total = Subtotal + ShippingCost;
            TotalItems = CartItems.Sum(item => item.Quantite);

            SaveCartToSession(CartItems);
        }

        private List<CartItem> GetCartFromSession()
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

        private void SaveCartToSession(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);

            // Sauvegarder dans la session
            HttpContext.Session.SetString("Cart", cartJson);

            // Sauvegarder aussi dans un cookie persistant (30 jours)
            Response.Cookies.Append("Cart", cartJson, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }
    }

    
}
