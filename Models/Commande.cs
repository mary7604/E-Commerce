namespace WebApplication1.Models
{
    public class Commande
    {
        public int Id { get; set; }
        public DateTime DateCommande { get; set; }
        public decimal MontantTotal { get; set; }
        public string Statut { get; set; } = "En attente";
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public decimal Total => MontantTotal;
    }
}