using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Produit> Produits { get; set; }
       
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Commande> Commandes { get; set; } = null!;
        public DbSet<LigneCommande> LignesCommande { get; set; }
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Avis> Avis { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration Produit
            modelBuilder.Entity<Produit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Prix).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Categorie).HasMaxLength(50);  
                entity.Property(e => e.DateAjout).HasDefaultValueSql("GETUTCDATE()");

               
            });

           
        }
    }
}