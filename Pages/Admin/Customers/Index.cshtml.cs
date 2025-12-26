using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin.Customers
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Client> Clients { get; set; } = new List<Client>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.Clients.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    query = query.Where(c =>
                        c.Nom.Contains(SearchTerm) ||
                        c.Email.Contains(SearchTerm));
                }

                Clients = await query
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
                Clients = new List<Client>();
            }
        }

        public int GetCommandesCount(int clientId)
        {
            try
            {
                return _context.Commandes.Count(c => c.ClientId == clientId);
            }
            catch
            {
                return 0;
            }
        }
    }
}