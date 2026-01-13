using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
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
        public Commande Commande { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var commande = await _context.Commandes
                .Include(c => c.LignesCommande)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
            {
                return NotFound();
            }

            // client connecté OU commande invitée
            var sessionClientId = HttpContext.Session.GetInt32("ClientId");

            if (commande.ClientId != null)
            {
                if (!sessionClientId.HasValue || sessionClientId.Value != commande.ClientId)
                    return Unauthorized();
            }

            Commande = commande;

            decimal total = commande.MontantTotal;
            decimal subtotal;
            decimal shipping;

            if (total >= 550)
            {
                subtotal = total;
                shipping = 0;
            }
            else
            {
                subtotal = total - 50;
                shipping = 50;
            }

            InvoiceHtml = _invoiceService.GenerateInvoiceHtml(
                commande,
                subtotal,
                shipping
            );

            return Page();
        }

    }
}
