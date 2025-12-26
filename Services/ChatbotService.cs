using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services
{
    public class ChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public ChatbotService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            try
            {
                // Récupérer les produits pour donner du contexte à l'IA
                var produits = await _context.Produits
                    .Where(p => p.Stock > 0)
                    .Select(p => new { p.Nom, p.Prix, p.Description, p.Stock })
                    .Take(20)
                    .ToListAsync();

                var catalogueInfo = string.Join("\n", produits.Select(p =>
                    $"- {p.Nom}: {p.Prix} MAD (Stock: {p.Stock}) - {p.Description}"));

                // Construire le prompt pour Ollama
                var systemPrompt = $@"Tu es un assistant virtuel pour MimiBout, une boutique e-commerce marocaine.

CATALOGUE ACTUEL (produits en stock):
{catalogueInfo}

RÈGLES IMPORTANTES:
- Réponds UNIQUEMENT en français
- Sois amical, professionnel et concis (maximum 3-4 phrases)
- Recommande des produits du catalogue ci-dessus
- Si un produit n'est pas dans le catalogue, dis-le poliment
- Utilise les prix exacts du catalogue
- Encourage à ajouter au panier
- Ne parle QUE de produits disponibles en stock

Question du client: {userMessage}

Réponse courte et utile:";

                // Appeler Ollama (local)
                var requestBody = new
                {
                    model = "llama3.2",
                    prompt = systemPrompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        num_predict = 200  // Limiter la longueur
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return "❌ Erreur: Ollama n'est pas démarré. Lancez 'ollama serve' dans PowerShell.";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);

                var assistantMessage = jsonResponse.RootElement
                    .GetProperty("response")
                    .GetString();

                return assistantMessage ?? "Je n'ai pas pu générer une réponse.";
            }
            catch (HttpRequestException)
            {
                return "❌ Impossible de se connecter à Ollama. Assurez-vous qu'Ollama est installé et lancé avec 'ollama serve'.";
            }
            catch (Exception ex)
            {
                return $"❌ Erreur: {ex.Message}";
            }
        }
    }
}
