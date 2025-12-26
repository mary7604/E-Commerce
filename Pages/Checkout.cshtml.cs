using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Text.Json;

namespace WebApplication1.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CheckoutModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string CartData { get; set; } = string.Empty;

        [BindProperty]
        public string TotalAmount { get; set; } = string.Empty;

        public bool OrderSuccess { get; set; }
        public int OrderId { get; set; }
        public string? ClientEmail { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // Vérifier si le client est connecté
            var clientIdStr = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientIdStr))
            {
                return RedirectToPage("/Account/Login", new { returnUrl = "/Checkout" });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Vérifier si le client est connecté
                var clientIdStr = HttpContext.Session.GetString("ClientId");
                if (string.IsNullOrEmpty(clientIdStr))
                {
                    return RedirectToPage("/Account/Login", new { returnUrl = "/Checkout" });
                }

                var clientId = int.Parse(clientIdStr);

                // Récupérer les informations du client
                var client = await _context.Clients.FindAsync(clientId);
                if (client == null)
                {
                    ErrorMessage = "Client introuvable";
                    return Page();
                }

                // Parser le panier
                var cartItems = JsonSerializer.Deserialize<List<CartItem>>(CartData);
                if (cartItems == null || !cartItems.Any())
                {
                    ErrorMessage = "Votre panier est vide";
                    return Page();
                }

                // Calculer le montant total
                decimal subtotal = cartItems.Sum(item => item.prix * item.quantity);
                decimal shipping = subtotal >= 500 ? 0 : 50;
                decimal total = subtotal + shipping;

                // Créer la commande
                var commande = new Commande
                {
                    ClientId = clientId,
                    DateCommande = DateTime.Now,
                    MontantTotal = total,
                    Statut = "En attente"
                };

                _context.Commandes.Add(commande);
                await _context.SaveChangesAsync();

                // Mettre à jour le stock des produits
                foreach (var item in cartItems)
                {
                    var produit = await _context.Produits.FindAsync(item.id);
                    if (produit != null)
                    {
                        produit.Stock -= item.quantity;
                        if (produit.Stock < 0) produit.Stock = 0;
                    }
                }

                await _context.SaveChangesAsync();

                // Succès
                OrderSuccess = true;
                OrderId = commande.Id;
                ClientEmail = client.Email;

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la création de la commande : {ex.Message}";
                return Page();
            }
        }

        // Classe pour désérialiser le panier
        public class CartItem
        {
            public int id { get; set; }
            public string nom { get; set; } = string.Empty;
            public decimal prix { get; set; }
            public int quantity { get; set; }
            public int stock { get; set; }
            public string? imageUrl { get; set; }
        }
    }
}
