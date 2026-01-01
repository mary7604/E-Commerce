using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Telephone { get; set; }

        public string? Adresse { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime DateInscription { get; set; } = DateTime.Now;

        public bool EstAdmin { get; set; } = false;

       
        public List<Commande> Commandes { get; set; } = new List<Commande>();
    }
}