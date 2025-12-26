using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Avis
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProduitId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Note { get; set; }

        [MaxLength(1000)]
        public string? Commentaire { get; set; }

        [Required]
        public DateTime DateAvis { get; set; } = DateTime.Now;

        public bool EstApprouve { get; set; } = false;

        // Navigation properties
        public Produit? Produit { get; set; }
        public Client? Client { get; set; }
    }
}