using System.Net;
using System.Net.Mail;

namespace WebApplication1.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal total)
        {
            try
            {
                // Configuration SMTP (à personnaliser)
                var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@boutique.com";
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUser = _configuration["Email:SmtpUser"] ?? "";
                var smtpPassword = _configuration["Email:SmtpPassword"] ?? "";

                // Si pas de configuration SMTP, simuler l'envoi
                if (string.IsNullOrEmpty(smtpUser))
                {
                    Console.WriteLine($"📧 EMAIL SIMULÉ envoyé à {toEmail}");
                    Console.WriteLine($"Commande #{orderId} - Montant: {total:N2} MAD");
                    return;
                }

                // Créer le message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Boutique E-Commerce"),
                    Subject = $"Confirmation de commande #{orderId}",
                    Body = GetEmailTemplate(customerName, orderId, total),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Configurer le client SMTP
                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUser, smtpPassword)
                };

                // Envoyer l'email
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine($"✅ Email envoyé avec succès à {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'envoi de l'email : {ex.Message}");
                // Ne pas bloquer la commande si l'email échoue
            }
        }

        private string GetEmailTemplate(string customerName, int orderId, decimal total)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: white;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
        }}
        .content {{
            padding: 30px;
        }}
        .order-box {{
            background: #f9fafb;
            border: 2px solid #e5e7eb;
            border-radius: 8px;
            padding: 20px;
            margin: 20px 0;
        }}
        .order-number {{
            font-size: 24px;
            font-weight: bold;
            color: #667eea;
            text-align: center;
            margin-bottom: 15px;
        }}
        .total {{
            font-size: 32px;
            font-weight: bold;
            color: #B12704;
            text-align: center;
            margin: 20px 0;
        }}
        .button {{
            display: inline-block;
            background: #667eea;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 6px;
            margin: 20px 0;
        }}
        .footer {{
            background: #f9fafb;
            padding: 20px;
            text-align: center;
            color: #6b7280;
            font-size: 14px;
        }}
        .check-icon {{
            width: 60px;
            height: 60px;
            background: #10b981;
            border-radius: 50%;
            margin: 0 auto 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 30px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='check-icon'>✓</div>
            <h1>Commande confirmée !</h1>
        </div>
        
        <div class='content'>
            <p style='font-size: 18px;'>Bonjour <strong>{customerName}</strong>,</p>
            
            <p>Merci pour votre commande ! Nous avons bien reçu votre paiement et votre commande est en cours de traitement.</p>
            
            <div class='order-box'>
                <div class='order-number'>Commande #{orderId:D6}</div>
                <div class='total'>{total:N2} MAD</div>
            </div>
            
            <p><strong>Que se passe-t-il ensuite ?</strong></p>
            <ul style='line-height: 1.8;'>
                <li>✅ Votre commande est confirmée</li>
                <li>📦 Préparation de votre colis sous 24h</li>
                <li>🚚 Expédition sous 2-3 jours ouvrés</li>
                <li>📧 Vous recevrez un email avec le numéro de suivi</li>
            </ul>
            
            <div style='text-align: center;'>
                <a href='https://localhost:7292/Account/Profile' class='button'>
                    Suivre ma commande
                </a>
            </div>
            
            <p style='margin-top: 30px; color: #6b7280; font-size: 14px;'>
                <strong>Besoin d'aide ?</strong><br>
                Contactez notre service client : support@boutique.com
            </p>
        </div>
        
        <div class='footer'>
            <p>© 2024 Boutique E-Commerce. Tous droits réservés.</p>
            <p>Cet email a été envoyé automatiquement, merci de ne pas y répondre.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
