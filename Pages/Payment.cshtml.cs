using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class PaymentModel : PageModel
    {
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }

        public IActionResult OnGet()
        {
            // Vérifier si le client est connecté
            var clientIdStr = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientIdStr))
            {
                return RedirectToPage("/Account/Login", new { returnUrl = "/Payment" });
            }

            // Récupérer les montants depuis la query string ou session
            Subtotal = decimal.Parse(Request.Query["subtotal"].ToString() ?? "0");
            Shipping = decimal.Parse(Request.Query["shipping"].ToString() ?? "0");
            Total = Subtotal + Shipping;

            return Page();
        }
    }
}
