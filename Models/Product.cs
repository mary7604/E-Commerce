using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Produit
    {
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public decimal Prix { get; set; }

        public int Stock { get; set; }

        public string? ImageUrl { get; set; }
    }
}