using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services
{
    public class CartService
    {
        private readonly IDistributedCache _redisCache;
        private readonly ApplicationDbContext _context;

        //constructeur
        public CartService(IDistributedCache redisCache, ApplicationDbContext context)
        {
            _redisCache = redisCache;
            _context = context;
        }

        // Récupérer le panier depuis Redis
        public async Task<List<CartItem>> GetCartAsync(string userId)
        {
            string cacheKey = $"cart_{userId}";
            var cartJson = await _redisCache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cartJson))
            {
                Console.WriteLine($" CACHE MISS - Panier vide pour {userId}");
                return new List<CartItem>();
            }

            Console.WriteLine($" CACHE HIT - Panier trouvé pour {userId}");
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        // Sauvegarder le panier dans Redis (expire après 30 jours)
        public async Task SaveCartAsync(string userId, List<CartItem> cart)
        {
            string cacheKey = $"cart_{userId}";
            var cartJson = JsonSerializer.Serialize(cart);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)//
            };

            await _redisCache.SetStringAsync(cacheKey, cartJson, options);
            Console.WriteLine($" Panier sauvegardé dans Redis pour {userId}");
        }

        // Ajouter un produit au panier avec validation de stock
        public async Task<(bool Success, string Message)> AddToCartAsync(string userId, CartItem newItem)
        {
            // Vérifier le stock disponible dans la base de données
            var produit = await _context.Produits.FindAsync(newItem.ProduitId);

            if (produit == null)
            {
                return (false, " Produit introuvable");
            }

            if (produit.Stock == 0)
            {
                return (false, $" Le produit '{produit.Nom}' est en rupture de stock");
            }

            var cart = await GetCartAsync(userId);
            var existingItem = cart.FirstOrDefault(i => i.ProduitId == newItem.ProduitId);

            int currentQuantityInCart = existingItem?.Quantite ?? 0;
            int requestedQuantity = newItem.Quantite;
            int totalQuantity = currentQuantityInCart + requestedQuantity;

            // Vérifier si la quantité totale dépasse le stock
            if (totalQuantity > produit.Stock)
            {
                if (currentQuantityInCart >= produit.Stock)
                {
                    return (false, $" Vous avez déjà le maximum disponible ({produit.Stock}) de '{produit.Nom}' dans votre panier");
                }
                else
                {
                    int available = produit.Stock - currentQuantityInCart;
                    return (false, $" Stock insuffisant ! Il reste seulement {available} unité(s) de '{produit.Nom}' disponible(s)");
                }
            }

            // Ajouter ou augmenter la quantité
            if (existingItem != null)
            {
                existingItem.Quantite += requestedQuantity;
            }
            else
            {
                cart.Add(newItem);
            }

            await SaveCartAsync(userId, cart);
            return (true, $"'{produit.Nom}' ajouté au panier avec succès !");
        }

        // Supprimer un produit du panier
        public async Task RemoveFromCartAsync(string userId, int produitId)
        {
            var cart = await GetCartAsync(userId);
            cart.RemoveAll(i => i.ProduitId == produitId);
            await SaveCartAsync(userId, cart);
        }

        // Mettre à jour la quantité avec validation de stock
        public async Task<(bool Success, string Message)> UpdateQuantityAsync(string userId, int produitId, int newQuantity)
        {
            // Vérifier le stock disponible
            var produit = await _context.Produits.FindAsync(produitId);

            if (produit == null)
            {
                return (false, " Produit introuvable");
            }

            if (newQuantity > produit.Stock)
            {
                return (false, $" Stock insuffisant ! Seulement {produit.Stock} unité(s) disponible(s) pour '{produit.Nom}'");
            }

            var cart = await GetCartAsync(userId);
            var item = cart.FirstOrDefault(i => i.ProduitId == produitId);

            if (item != null)
            {
                if (newQuantity <= 0)
                {
                    cart.Remove(item);
                    await SaveCartAsync(userId, cart);
                    return (true, $"🗑️ '{produit.Nom}' retiré du panier");
                }
                else
                {
                    item.Quantite = newQuantity;
                    await SaveCartAsync(userId, cart);
                    return (true, $" Quantité de '{produit.Nom}' mise à jour");
                }
            }

            return (false, " Produit non trouvé dans le panier");
        }

        // Vider le panier
        public async Task ClearCartAsync(string userId)
        {
            string cacheKey = $"cart_{userId}";
            await _redisCache.RemoveAsync(cacheKey);
            Console.WriteLine($" Panier vidé pour {userId}");
        }

        // Obtenir le nombre total d'articles
        public async Task<int> GetCartCountAsync(string userId)
        {
            var cart = await GetCartAsync(userId);
            return cart.Sum(i => i.Quantite);
        }

        // Calculer le sous-total
        public async Task<decimal> GetSubtotalAsync(string userId)
        {
            var cart = await GetCartAsync(userId);
            return cart.Sum(i => i.Prix * i.Quantite);
        }

        // Valider tout le panier avant validation de commande
        public async Task<(bool IsValid, List<string> Errors)> ValidateCartStockAsync(string userId)
        {
            var cart = await GetCartAsync(userId);
            var errors = new List<string>();
            bool hasChanges = false;

            foreach (var item in cart.ToList())
            {
                var produit = await _context.Produits.FindAsync(item.ProduitId);

                if (produit == null)
                {
                    cart.Remove(item);
                    errors.Add($" Le produit '{item.Nom}' n'existe plus");
                    hasChanges = true;
                    continue;
                }

                if (produit.Stock == 0)
                {
                    cart.Remove(item);
                    errors.Add($" '{produit.Nom}' est en rupture de stock et a été retiré du panier");
                    hasChanges = true;
                    continue;
                }

                if (item.Quantite > produit.Stock)
                {
                    item.Quantite = produit.Stock;
                    errors.Add($" La quantité de '{produit.Nom}' a été ajustée à {produit.Stock} (stock disponible)");
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await SaveCartAsync(userId, cart);
            }

            return (errors.Count == 0, errors);
        }
    }

    // Modèle CartItem
    public class CartItem
    {
        public int ProduitId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public int Quantite { get; set; }
        public string? ImageUrl { get; set; }
    }
}