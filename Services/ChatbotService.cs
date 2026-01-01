using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services
{
    public class ChatbotService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public ChatbotService(
            IConfiguration configuration,
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            var apiKey = _configuration["GoogleGemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return " Clé API Gemini manquante.";

            // On prend tous les produits avec stock > 0
            var produits = await _context.Produits
                .Where(p => p.Stock > 0)
                .Select(p => new { p.Nom, p.Prix, p.Stock })
                .ToListAsync();

            // Création du catalogue pour le prompt
            var catalogue = string.Join("\n",
                produits.Select(p => $"- {p.Nom} : {p.Prix} MAD (Stock {p.Stock})"));

            // Prompt 
            var prompt = $"""
    Tu es l'assistant officiel de MimiBout (e-commerce marocain).
    
    Catalogue disponible :
    {catalogue}

    Règles :
    - Réponds en français
    - Sois clair et court
    - Propose uniquement les produits listés ci-dessus
    - N'invente pas de produits qui ne sont pas dans le catalogue
    - Encourage l'achat

    Question client : {userMessage}
    """;

            var body = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        }
            };

        
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $" Erreur Gemini {response.StatusCode} : {error}";
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return json.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Aucune réponse générée.";
        }
    }
    }
