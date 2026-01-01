using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Models;

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
        public string NomClient { get; set; } = string.Empty;

        [BindProperty]
        public string EmailClient { get; set; } = string.Empty;

        [BindProperty]
        public string TelephoneClient { get; set; } = string.Empty;

        [BindProperty]
        public string AdresseLivraison { get; set; } = string.Empty;

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }

        public void OnGet()
        {
            LoadCart();
            
            // Pré-remplir avec les infos du client connecté si disponible
            var clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId.HasValue)
            {
                var client = _context.Clients.Find(clientId.Value);
                if (client != null)
                {
                    NomClient = $"{client.Prenom} {client.Nom}";
                    EmailClient = client.Email;
                    TelephoneClient = client.Telephone ?? "";
                    AdresseLivraison = client.Adresse ?? "";
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadCart();
                return Page();
            }

            // Récupérer le panier
            var cart = GetCartFromSessionOrCookie();

            if (cart.Count == 0)
            {
                TempData["Error"] = "Votre panier est vide !";
                return RedirectToPage("/Index");
            }

            // Vérifier le stock avant de créer la commande
            foreach (var item in cart)
            {
                var produit = await _context.Produits.FindAsync(item.ProduitId);
                if (produit == null || produit.Stock < item.Quantite)
                {
                    TempData["Error"] = $"Stock insuffisant pour {item.Nom}";
                    LoadCart();
                    return Page();
                }
            }

            // Calculer les totaux
            var subtotal = cart.Sum(item => item.Prix * item.Quantite);
            var shippingCost = subtotal >= 500 ? 0 : 50;
            var total = subtotal + shippingCost;

            // Récupérer l'ID du client connecté
            var clientId = HttpContext.Session.GetInt32("ClientId");

            // Créer la commande
            var commande = new Commande
            {
                ClientId = clientId,  
                NomClient = NomClient,
                EmailClient = EmailClient,
                TelephoneClient = TelephoneClient,
                AdresseLivraison = AdresseLivraison,
                DateCommande = DateTime.Now,
                Statut = "En attente",
                MontantTotal = total,
                LignesCommande = cart.Select(item => new LigneCommande
                {
                    NomProduit = item.Nom,
                    Quantite = item.Quantite,
                    PrixUnitaire = item.Prix
                }).ToList()
            };

            _context.Commandes.Add(commande);

            // Décrémenter le stock
            foreach (var item in cart)
            {
                var produit = await _context.Produits.FindAsync(item.ProduitId);
                if (produit != null)
                {
                    produit.Stock -= item.Quantite;
                }
            }

            await _context.SaveChangesAsync();

            // Vider le panier
            ClearCart();

            // Rediriger vers la page de paiement
            TempData["Message"] = "Commande créée avec succès ! Procédez au paiement.";
            return RedirectToPage("/Payment", new { id = commande.Id });
        }

        private void LoadCart()
        {
            var cart = GetCartFromSessionOrCookie();
            CartItems = cart;
            Subtotal = cart.Sum(item => item.Prix * item.Quantite);
            ShippingCost = Subtotal >= 500 ? 0 : 50;
            Total = Subtotal + ShippingCost;
        }

        private List<CartItem> GetCartFromSessionOrCookie()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            
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

        private void ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            Response.Cookies.Delete("Cart");
        }
    }
}