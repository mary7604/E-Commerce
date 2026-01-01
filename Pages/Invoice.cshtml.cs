using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class InvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly InvoiceService _invoiceService;

        public InvoiceModel(ApplicationDbContext context, InvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        public string InvoiceHtml { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Récupérer la commande
            var commande = await _context.Commandes
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
                return NotFound();

            // client connecté OU commande invitée
            var sessionClientId = HttpContext.Session.GetInt32("ClientId");

            if (commande.ClientId != null)
            {
                if (!sessionClientId.HasValue || sessionClientId.Value != commande.ClientId)
                    return Unauthorized();
            }

            // Calcul livraison & sous-total 
            var shipping = commande.MontantTotal >= 500 ? 0 : 50;
            var subtotal = commande.MontantTotal - shipping;

            // Générer la facture HTML
            InvoiceHtml = _invoiceService.GenerateInvoiceHtml(
                orderId: commande.Id,
                customerName: commande.Client != null
                    ? $"{commande.Client.Prenom} {commande.Client.Nom}"
                    : commande.NomClient,
                customerEmail: commande.EmailClient,
                customerAddress: commande.AdresseLivraison,
                subtotal: subtotal,
                shipping: shipping,
                total: commande.MontantTotal,
                orderDate: commande.DateCommande
            );

            return Page();
        }
    }
}
