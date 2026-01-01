using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class UsersController : Controller
    {
        // Liste temporaire 
        private static List<User> users = new List<User>
        {
            new User { Id = 1, FirstName = "Ahmed", LastName = "Benali", Email = "ahmed@example.com", Age = 25 },
            new User { Id = 2, FirstName = "Fatima", LastName = "Alami", Email = "fatima@example.com", Age = 30 }
        };

        // GET: Afficher la liste
        public IActionResult Index()
        {
            return View(users);
        }

        // GET: Formulaire de création
        public IActionResult Create()
        {
            return View();
        }

        // POST: Créer un utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User user)
        {
            if (ModelState.IsValid)
            {
                user.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
                users.Add(user);
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Détails d'un utilisateur
        public IActionResult Details(int id)
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            return View(user);
        }
    }
}