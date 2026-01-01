using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ClientAuthService
    {
        private readonly ApplicationDbContext _context;

        public ClientAuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task<Client?> Register(string nom, string prenom, string email, string password, string? telephone = null, string? adresse = null)
        {
            // Vérifier si l'email existe déjà
            if (await _context.Clients.AnyAsync(c => c.Email == email))
            {
                return null;
            }

            var client = new Client
            {
                Nom = nom,
                Prenom = prenom,
                Email = email,
                PasswordHash = HashPassword(password),
                Telephone = telephone,
                Adresse = adresse,
                DateInscription = DateTime.Now,
                EstAdmin = false
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return client;
        }

        public async Task<Client?> Login(string email, string password)
        {
            var passwordHash = HashPassword(password);
            return await _context.Clients
                .FirstOrDefaultAsync(c => c.Email == email && c.PasswordHash == passwordHash);
        }

        public async Task<Client?> GetClientById(int id)
        {
            return await _context.Clients.FindAsync(id);
        }

        public async Task<bool> UpdateProfile(int clientId, string nom, string prenom, string? telephone, string? adresse)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return false;

            client.Nom = nom;
            client.Prenom = prenom;
            client.Telephone = telephone;
            client.Adresse = adresse;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePassword(int clientId, string oldPassword, string newPassword)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return false;

            var oldPasswordHash = HashPassword(oldPassword);
            if (client.PasswordHash != oldPasswordHash)
            {
                return false;
            }

            client.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Commande>> GetClientCommandes(int clientId)
        {
            return await _context.Commandes
                .Where(c => c.ClientId == clientId)
                .OrderByDescending(c => c.DateCommande)
                .ToListAsync();
        }
    }
}