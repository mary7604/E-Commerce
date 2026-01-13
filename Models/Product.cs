using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Produit
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le prix est obligatoire")]
        [Range(0.01, 999999.99, ErrorMessage = "Le prix doit être positif")]
        public decimal Prix { get; set; }

        [Required(ErrorMessage = "Le stock est obligatoire")]
        [Range(0, int.MaxValue, ErrorMessage = "Le stock ne peut pas être négatif")]
        public int Stock { get; set; }

        [StringLength(50)]
        public string? Categorie { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime DateAjout { get; set; } = DateTime.Now;

        public DateTime? DateModification { get; set; }
    }
}