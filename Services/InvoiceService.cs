using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class InvoiceService
    {
      
        public string GenerateInvoiceHtml(
            Commande commande,  //  Passer toute la commande
            decimal subtotal,
            decimal shipping)
        {
            var invoiceNumber = $"FAC-{commande.DateCommande:yyyyMM}-{commande.Id:D6}";
            var total = commande.MontantTotal;

            
            var productsHtml = new StringBuilder();

            if (commande.LignesCommande != null && commande.LignesCommande.Any())
            {
                foreach (var ligne in commande.LignesCommande)
                {
                    var lineTotal = ligne.PrixUnitaire * ligne.Quantite;
                    productsHtml.Append($@"
                <tr>
                    <td>{ligne.NomProduit}</td>
                    <td style='text-align: center;'>{ligne.Quantite}</td>
                    <td style='text-align: right;'>{ligne.PrixUnitaire:N2} MAD</td>
                    <td style='text-align: right;'><strong>{lineTotal:N2} MAD</strong></td>
                </tr>");
                }
            }
            else
            {
                productsHtml.Append(@"
                <tr>
                    <td colspan='4' style='text-align: center; padding: 40px; color: #6b7280;'>
                        Aucun produit dans cette commande
                    </td>
                </tr>");
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Facture #{invoiceNumber}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            padding: 40px;
            background: white;
        }}
        .invoice-container {{
            max-width: 800px;
            margin: 0 auto;
            border: 2px solid #e5e7eb;
            padding: 40px;
        }}
        .header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 40px;
            padding-bottom: 20px;
            border-bottom: 3px solid #667eea;
        }}
        .company-info {{
            flex: 1;
        }}
        .company-name {{
            font-size: 32px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 10px;
        }}
        .invoice-info {{
            text-align: right;
        }}
        .invoice-number {{
            font-size: 24px;
            font-weight: bold;
            color: #111827;
            margin-bottom: 10px;
        }}
        .info-section {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 40px;
        }}
        .info-box {{
            flex: 1;
        }}
        .info-box h3 {{
            font-size: 14px;
            color: #6b7280;
            text-transform: uppercase;
            margin-bottom: 10px;
        }}
        .info-box p {{
            margin: 5px 0;
            color: #111827;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 30px;
        }}
        thead {{
            background: #f9fafb;
        }}
        th {{
            padding: 12px;
            text-align: left;
            font-weight: 600;
            color: #374151;
            border-bottom: 2px solid #e5e7eb;
        }}
        td {{
            padding: 12px;
            border-bottom: 1px solid #f3f4f6;
        }}
        .totals {{
            float: right;
            width: 300px;
            margin-top: 20px;
        }}
        .totals-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
        }}
        .totals-row.total {{
            border-top: 2px solid #e5e7eb;
            margin-top: 10px;
            padding-top: 15px;
            font-size: 20px;
            font-weight: bold;
            color: #B12704;
        }}
        .footer {{
            margin-top: 60px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            text-align: center;
            color: #6b7280;
            font-size: 12px;
        }}
        .stamp {{
            margin-top: 40px;
            text-align: right;
            color: #6b7280;
            font-style: italic;
        }}
        .badge {{
            display: inline-block;
            padding: 4px 12px;
            border-radius: 6px;
            font-size: 12px;
            font-weight: bold;
        }}
        .badge-paid {{
            background: #d1fae5;
            color: #065f46;
        }}
        .badge-pending {{
            background: #fef3c7;
            color: #92400e;
        }}
        @media print {{
            body {{
                padding: 0;
            }}
            .invoice-container {{
                border: none;
            }}
        }}
    </style>
</head>
<body>
    <div class='invoice-container'>
        <!-- Header -->
        <div class='header'>
            <div class='company-info'>
                <div class='company-name'>🛍️ MimiBout</div>
                <p>E-Commerce Solution</p>
                <p>Casablanca, Maroc</p>
                <p>Tél: +212 5XX XX XX XX</p>
                <p>Email: contact@mimibout.com</p>
            </div>
            <div class='invoice-info'>
                <div class='invoice-number'>FACTURE</div>
                <p><strong>{invoiceNumber}</strong></p>
                <p>Date: {commande.DateCommande:dd/MM/yyyy}</p>
                <p>Commande: #{commande.Id:D6}</p>
                <p><span class='badge {(commande.Statut == "Payée" ? "badge-paid" : "badge-pending")}'>{commande.Statut}</span></p>
            </div>
        </div>

        <!-- Client Info -->
        <div class='info-section'>
            <div class='info-box'>
                <h3>Facturé à</h3>
                <p><strong>{commande.NomClient}</strong></p>
                <p>{commande.EmailClient}</p>
                <p>{commande.TelephoneClient}</p>
                <p>{commande.AdresseLivraison}</p>
            </div>
            <div class='info-box'>
                <h3>Détails de paiement</h3>
                <p><strong>Statut:</strong> {commande.Statut}</p>
                <p><strong>Mode:</strong> {(string.IsNullOrEmpty(commande.ModePaiement) ? "Non spécifié" : commande.ModePaiement)}</p>
                <p><strong>Date:</strong> {commande.DateCommande:dd/MM/yyyy HH:mm}</p>
            </div>
        </div>

        <!-- Items Table -->
        <table>
            <thead>
                <tr>
                    <th>Description</th>
                    <th style='text-align: center;'>Quantité</th>
                    <th style='text-align: right;'>Prix unitaire</th>
                    <th style='text-align: right;'>Total</th>
                </tr>
            </thead>
            <tbody>
                {productsHtml}
            </tbody>
        </table>

        <!-- Totals -->
        <div class='totals'>
            <div class='totals-row'>
                <span>Sous-total:</span>
                <strong>{subtotal:N2} MAD</strong>
            </div>
            <div class='totals-row'>
                <span>Livraison:</span>
                <strong>{(shipping == 0 ? "Gratuite " : $"{shipping:N2} MAD")}</strong>
            </div>
            <div class='totals-row total'>
                <span>TOTAL TTC:</span>
                <strong>{total:N2} MAD</strong>
            </div>
        </div>

        <div style='clear: both;'></div>

        <!-- Stamp -->
        <div class='stamp'>
            <p>Document généré automatiquement le {DateTime.Now:dd/MM/yyyy à HH:mm}</p>
            <p>Signature électronique validée ✓</p>
        </div>

        <!-- Footer -->
        <div class='footer'>
            <p><strong>Conditions de paiement:</strong> {(commande.ModePaiement == "Paiement à la livraison" ? "Paiement à la livraison" : "Paiement comptant")}</p>
            <p><strong>Conditions de livraison:</strong> Livraison sous 2-3 jours ouvrés</p>
            <p style='margin-top: 20px;'>
                Merci pour votre confiance !<br>
                Pour toute question, contactez-nous à support@mimibout.com
            </p>
            <p style='margin-top: 20px; font-size: 10px;'>
                MimiBout - SARL au capital de 100 000 MAD<br>
                RC: Casablanca 123456 - IF: 12345678 - ICE: 000123456789012<br>
                Siège social: 123 Boulevard Mohammed V, Casablanca, Maroc
            </p>
        </div>
    </div>
</body>
</html>";
        }

        // Pour sauvegarder la facture
        public async Task<string> SaveInvoiceAsync(string html, int orderId)
        {
            try
            {
                var fileName = $"Facture_{orderId:D6}.html";
                var filePath = Path.Combine("wwwroot", "invoices", fileName);

                // Créer le dossier s'il n'existe pas
                Directory.CreateDirectory(Path.Combine("wwwroot", "invoices"));

                // Sauvegarder le fichier
                await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);

                return $"/invoices/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Erreur lors de la sauvegarde de la facture : {ex.Message}");
                return string.Empty;
            }
        }
    }
}