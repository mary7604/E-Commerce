using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Commande
    {
        public int Id { get; set; }

        public DateTime DateCommande { get; set; } = DateTime.Now;

        public decimal MontantTotal { get; set; }

        public string Statut { get; set; } = "En attente";

       
        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        // Informations de livraison 
        [Required]
        public string NomClient { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string EmailClient { get; set; } = string.Empty;

        [Required]
        public string TelephoneClient { get; set; } = string.Empty;

        [Required]
        public string AdresseLivraison { get; set; } = string.Empty;

        // Mode de paiement
        public string? ModePaiement { get; set; }

        // Lignes de commande
        public List<LigneCommande> LignesCommande { get; set; } = new List<LigneCommande>();

        public decimal Total => MontantTotal;
    }
}