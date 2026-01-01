using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class LigneCommande
    {
        public int Id { get; set; }

        public int CommandeId { get; set; }
        public Commande? Commande { get; set; }

        [Required]
        public string NomProduit { get; set; } = string.Empty;

        public int Quantite { get; set; }

        public decimal PrixUnitaire { get; set; }

        public decimal Total => Quantite * PrixUnitaire;
    }
}
