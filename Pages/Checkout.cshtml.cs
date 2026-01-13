using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;

        public CheckoutModel(ApplicationDbContext context, CartService cartService)
        {
            _context = context;
            _cartService = cartService;
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

        public async Task<IActionResult> OnGetAsync()
        {
            //  VÉRIFIER SI LE CLIENT EST CONNECTÉ
            var clientId = HttpContext.Session.GetInt32("ClientId");

            if (!clientId.HasValue)
            {
                // Client NON connecté = Redirection vers Login
                TempData["Error"] = " Vous devez vous connecter pour passer commande";
                TempData["ReturnUrl"] = "/Checkout";  // Pour revenir après connexion
                return RedirectToPage("/Account/Login");
            }

            //  Client connecté → Charger ses informations
            var client = await _context.Clients.FindAsync(clientId.Value);

            if (client != null)
            {
                // PRÉ-REMPLIR le formulaire automatiquement
                NomClient = $"{client.Prenom} {client.Nom}";
                EmailClient = client.Email;
                TelephoneClient = client.Telephone ?? "";
                AdresseLivraison = client.Adresse ?? "";

                Console.WriteLine($" Formulaire pré-rempli pour : {NomClient}");
            }

            await LoadCartAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //  DOUBLE VÉRIFICATION : Client toujours connecté ?
            var clientId = HttpContext.Session.GetInt32("ClientId");

            if (!clientId.HasValue)
            {
                TempData["Error"] = " Session expirée. Veuillez vous reconnecter.";
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                await LoadCartAsync();
                return Page();
            }

            //  Récupérer le panier depuis Redis
            string userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);

            if (cart.Count == 0)
            {
                TempData["Error"] = "Votre panier est vide !";
                return RedirectToPage("/Index");
            }

            // Valider le stock avant de créer la commande
            var validation = await _cartService.ValidateCartStockAsync(userId);
            if (!validation.IsValid)
            {
                TempData["Error"] = string.Join("<br>", validation.Errors);
                await LoadCartAsync();
                return Page();
            }

            // Calculer les totaux
            var subtotal = cart.Sum(item => item.Prix * item.Quantite);
            var shippingCost = subtotal >= 500 ? 0 : 50;
            var total = subtotal + shippingCost;

            //  Créer la commande (ClientId toujours rempli)
            var commande = new Commande
            {
                ClientId = clientId.Value,  //  Toujours rempli maintenant
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

            //  Vider le panier dans Redis
            await _cartService.ClearCartAsync(userId);

            // Rediriger vers la page de paiement
            TempData["Message"] = " Commande créée avec succès ! Procédez au paiement.";
            return RedirectToPage("/Payment", new { id = commande.Id });
        }

        private async Task LoadCartAsync()
        {
            string userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);

            CartItems = cart;
            Subtotal = cart.Sum(item => item.Prix * item.Quantite);
            ShippingCost = Subtotal >= 500 ? 0 : 50;
            Total = Subtotal + ShippingCost;
        }

        private string GetUserId()
        {
           

            // Utilisateur connecté
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.Identity.Name ?? "guest";
            }

            // visiteur Cookie
            string? userId = Request.Cookies["GuestCartId"];

            if (string.IsNullOrEmpty(userId))
            {
                userId = $"guest_{Guid.NewGuid()}";

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
