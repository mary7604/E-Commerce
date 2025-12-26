using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(50)]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(50)]
        [Display(Name = "Nom")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [Display(Name = "Téléphone")]
        public string? Phone { get; set; }

        // Alias pour PhoneNumber (pour compatibilité)
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [Display(Name = "Numéro de téléphone")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Range(18, 120, ErrorMessage = "L'âge doit être entre 18 et 120 ans")]
        [Display(Name = "Âge")]
        public int Age { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date de naissance")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(200)]
        [Display(Name = "Adresse")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Ville")]
        public string? City { get; set; }

        [StringLength(10)]
        [Display(Name = "Code postal")]
        public string? PostalCode { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}