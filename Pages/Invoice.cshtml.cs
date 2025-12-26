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
            try
            {
                // Récupérer la commande
                var commande = await _context.Commandes
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (commande == null)
                {
                    return NotFound();
                }

                // Vérifier que c'est bien le client de la commande
                var clientIdStr = HttpContext.Session.GetString("ClientId");
                if (string.IsNullOrEmpty(clientIdStr) || int.Parse(clientIdStr) != commande.ClientId)
                {
                    return Unauthorized();
                }

                // Calculer sous-total et livraison
                var subtotal = commande.MontantTotal - (commande.MontantTotal >= 550 ? 0 : 50);
                var shipping = commande.MontantTotal >= 550 ? 0 : 50;

                // Générer la facture HTML
                InvoiceHtml = _invoiceService.GenerateInvoiceHtml(
                    orderId: commande.Id,
                    customerName: commande.Client?.Nom ?? "Client",
                    customerEmail: commande.Client?.Email ?? "",
                    customerAddress: commande.Client?.Adresse ?? "",
                    subtotal: subtotal,
                    shipping: shipping,
                    total: commande.MontantTotal,
                    orderDate: commande.DateCommande
                );

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur : {ex.Message}");
                return NotFound();
            }
        }
    }
}
