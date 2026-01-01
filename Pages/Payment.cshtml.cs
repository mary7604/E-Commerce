using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Services;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class PaymentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public PaymentModel(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public Commande Commande { get; set; } = null!;
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }

        [BindProperty]
        public string PaymentMethod { get; set; } = "carte";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var commande = await _context.Commandes
                .Include(c => c.LignesCommande)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
            {
                return NotFound();
            }

            Commande = commande;
            CalculateTotals();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var commande = await _context.Commandes
                .Include(c => c.LignesCommande)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
            {
                return NotFound();
            }

            // Mettre à jour le mode de paiement
            commande.ModePaiement = PaymentMethod switch
            {
                "carte" => "Carte bancaire",
                "especes" => "Paiement à la livraison",
                "virement" => "Virement bancaire",
                _ => "Non spécifié"
            };

            // Mettre à jour le statut
            if (PaymentMethod == "carte" || PaymentMethod == "virement")
            {
                commande.Statut = "Payée";
            }
            else
            {
                commande.Statut = "En attente de paiement";
            }

            await _context.SaveChangesAsync();

            // Envoyer l'email de confirmation
            try
            {
                var clientName = commande.NomClient;
                var clientEmail = commande.EmailClient;

                if (!string.IsNullOrEmpty(clientEmail))
                {
                    await _emailService.SendOrderConfirmationAsync(
                        clientEmail,
                        clientName,
                        commande.Id,
                        commande.MontantTotal
                    );

                    TempData["Message"] = "Paiement confirmé ! Un email de confirmation a été envoyé à " + clientEmail;
                }
                else
                {
                    TempData["Message"] = "Paiement confirmé !";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur envoi email: {ex.Message}");
                TempData["Message"] = "Paiement confirmé !";
            }

            // Rediriger vers la facture
            return RedirectToPage("/Invoice", new { id = commande.Id });
        }

        private void CalculateTotals()
        {
            Total = Commande.MontantTotal;

            // Calculer shipping (50 si total < 550, sinon 0)
            if (Total >= 550)
            {
                Subtotal = Total;
                ShippingCost = 0;
            }
            else
            {
                Subtotal = Total - 50;
                ShippingCost = 50;
            }
        }
    }
}
