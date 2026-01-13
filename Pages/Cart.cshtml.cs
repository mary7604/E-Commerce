using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class CartModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;  //  Service Redis

        public CartModel(ApplicationDbContext context, CartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public int TotalItems { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCartAsync();
        }

        // Supprimer un produit du panier
        public async Task<IActionResult> OnPostRemoveAsync(int id)
        {
            string userId = GetUserId();
            await _cartService.RemoveFromCartAsync(userId, id);

            TempData["Message"] = "Produit retiré du panier";
            return RedirectToPage();
        }

        // Mettre à jour la quantité
        public async Task<IActionResult> OnPostUpdateQuantityAsync(int id, string action)
        {
            string userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);
            var item = cart.FirstOrDefault(i => i.ProduitId == id);

            if (item != null)
            {
                var produit = await _context.Produits.FindAsync(id);
                if (produit != null)
                {
                    if (action == "increase" && item.Quantite < produit.Stock)
                    {
                        await _cartService.UpdateQuantityAsync(userId, id, item.Quantite + 1);
                        TempData["Message"] = "Quantité augmentée";
                    }
                    else if (action == "decrease")
                    {
                        if (item.Quantite > 1)
                        {
                            await _cartService.UpdateQuantityAsync(userId, id, item.Quantite - 1);
                            TempData["Message"] = "Quantité diminuée";
                        }
                        else
                        {
                            await _cartService.RemoveFromCartAsync(userId, id);
                            TempData["Message"] = "Produit retiré du panier";
                        }
                    }
                }
            }

            return RedirectToPage();
        }

        //  Charger le panier depuis Redis
        private async Task LoadCartAsync()
        {
            string userId = GetUserId();

            //  Récupérer depuis Redis
            var cart = await _cartService.GetCartAsync(userId);

            // Récupérer les détails des produits depuis la BDD
            var produitIds = cart.Select(c => c.ProduitId).ToList();
            var produits = await _context.Produits
                .Where(p => produitIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Mettre à jour les informations du panier
            CartItems = cart.Select(c => {
                if (produits.TryGetValue(c.ProduitId, out var produit))
                {
                    c.Nom = produit.Nom;
                    c.Prix = produit.Prix;
                    c.ImageUrl = produit.ImageUrl;

                    // Si la quantité dépasse le stock, ajuster
                    if (c.Quantite > produit.Stock)
                    {
                        c.Quantite = produit.Stock;
                    }
                }
                return c;
            }).ToList();

            // Sauvegarder les ajustements dans Redis
            await _cartService.SaveCartAsync(userId, CartItems);

            // Calculer les totaux
            Subtotal = CartItems.Sum(item => item.Prix * item.Quantite);
            ShippingCost = Subtotal >= 500 ? 0 : 50;
            Total = Subtotal + ShippingCost;
            TotalItems = CartItems.Sum(item => item.Quantite);
        }

        // Obtenir l'ID utilisateur
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
    }
}
