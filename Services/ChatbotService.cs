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

        public async Task<ChatResponse> GetChatResponseAsync(string userMessage)
        {
            var apiKey = _configuration["GoogleGemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return new ChatResponse
                {
                    Text = "❌ Clé API Gemini manquante.",
                    Products = new List<ProductCard>()
                };

            // Récupérer tous les produits avec stock
            var produits = await _context.Produits
                .Where(p => p.Stock > 0)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Nom = p.Nom,
                    Prix = p.Prix,
                    Stock = p.Stock,
                    ImageUrl = p.ImageUrl,
                    Categorie = p.Categorie
                })
                .ToListAsync();

            // Catalogue pour le prompt
            var catalogue = string.Join("\n",
                produits.Select(p => $"- ID:{p.Id} | {p.Nom} | {p.Prix} MAD | Stock:{p.Stock} | Catégorie:{p.Categorie}"));

            var prompt = $"""
Tu es l'assistant officiel de MimiBout (e-commerce marocain).

Catalogue disponible :
{catalogue}

IMPORTANT : Quand tu recommandes des produits, donne leurs IDs (format: ID:123).

Règles :
- Réponds en français, de manière naturelle et amicale
- Sois concis (max 3-4 lignes)
- Recommande 2-3 produits maximum pertinents
- Inclus les IDs des produits recommandés au format ID:123
- Encourage l'achat
- Si la question ne concerne pas les produits, réponds normalement

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

            // Retry avec délai exponentiel
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(body),
                            Encoding.UTF8,
                            "application/json")
                    };

                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        var geminiText = json.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString() ?? "Aucune réponse générée.";

                        // Extraire les IDs de produits mentionnés
                        var productIds = ExtractProductIds(geminiText);
                        var recommendedProducts = new List<ProductCard>();

                        foreach (var id in productIds)
                        {
                            var produit = produits.FirstOrDefault(p => p.Id == id);
                            if (produit != null)
                            {
                                recommendedProducts.Add(new ProductCard
                                {
                                    Id = produit.Id,
                                    Nom = produit.Nom,
                                    Prix = produit.Prix,
                                    ImageUrl = produit.ImageUrl ?? "/images/default.jpg",
                                    Stock = produit.Stock
                                });
                            }
                        }

                        // Nettoyer le texte (enlever les IDs)
                        var cleanText = System.Text.RegularExpressions.Regex.Replace(
                            geminiText,
                            @"ID:\d+\s*\|?\s*",
                            ""
                        );

                        return new ChatResponse
                        {
                            Text = cleanText.Trim(),
                            Products = recommendedProducts
                        };
                    }

                    // Si erreur 503, réessayer
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            var delaySeconds = Math.Pow(2, retryCount);
                            Console.WriteLine($"⏳ Gemini surchargé. Retry {retryCount}/{maxRetries} dans {delaySeconds}s...");
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                            continue;
                        }
                    }

                    var error = await response.Content.ReadAsStringAsync();
                    return new ChatResponse
                    {
                        Text = $"❌ Erreur Gemini {response.StatusCode}",
                        Products = new List<ProductCard>()
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur : {ex.Message}");

                    // Fallback avec détection mots-clés
                    return GetFallbackResponse(userMessage, produits);
                }
            }

            return new ChatResponse
            {
                Text = "❌ Le service IA est temporairement indisponible. Parcourez notre catalogue ci-dessous !",
                Products = produits.Take(3).Select(p => new ProductCard
                {
                    Id = p.Id,
                    Nom = p.Nom,
                    Prix = p.Prix,
                    ImageUrl = p.ImageUrl ?? "/images/default.jpg",
                    Stock = p.Stock
                }).ToList()
            };
        }

        // Extraire les IDs des produits du texte Gemini
        private List<int> ExtractProductIds(string text)
        {
            var ids = new List<int>();
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"ID:(\d+)");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        // Réponse de secours avec détection mots-clés
        private ChatResponse GetFallbackResponse(string userMessage, List<ProductDto> produits)
        {
            var messageLower = userMessage.ToLower();
            var recommendedProducts = new List<ProductCard>();
            string text = "";

            if (messageLower.Contains("casque") || messageLower.Contains("audio") || messageLower.Contains("écouteur"))
            {
                text = "🎧 Découvrez nos casques audio premium disponibles :";
                recommendedProducts = produits
                    .Where(p => p.Nom.ToLower().Contains("casque") || p.Nom.ToLower().Contains("audio"))
                    .Take(3)
                    .Select(p => new ProductCard
                    {
                        Id = p.Id,
                        Nom = p.Nom,
                        Prix = p.Prix,
                        ImageUrl = p.ImageUrl ?? "/images/default.jpg",
                        Stock = p.Stock
                    })
                    .ToList();
            }
            else if (messageLower.Contains("laptop") || messageLower.Contains("ordinateur") || messageLower.Contains("pc"))
            {
                text = "💻 Voici notre sélection d'ordinateurs portables :";
                recommendedProducts = produits
                    .Where(p => p.Nom.ToLower().Contains("laptop") || p.Nom.ToLower().Contains("ordinateur"))
                    .Take(3)
                    .Select(p => new ProductCard
                    {
                        Id = p.Id,
                        Nom = p.Nom,
                        Prix = p.Prix,
                        ImageUrl = p.ImageUrl ?? "/images/default.jpg",
                        Stock = p.Stock
                    })
                    .ToList();
            }
            else
            {
                text = "🛍️ Découvrez notre sélection du moment :";
                recommendedProducts = produits
                    .Take(3)
                    .Select(p => new ProductCard
                    {
                        Id = p.Id,
                        Nom = p.Nom,
                        Prix = p.Prix,
                        ImageUrl = p.ImageUrl ?? "/images/default.jpg",
                        Stock = p.Stock
                    })
                    .ToList();
            }

            return new ChatResponse
            {
                Text = text,
                Products = recommendedProducts
            };
        }
    }

    // Modèles de réponse
    public class ChatResponse
    {
        public string Text { get; set; } = string.Empty;
        public List<ProductCard> Products { get; set; } = new();
    }

    public class ProductCard
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Stock { get; set; }
    }

    // DTO pour éviter les types anonymes
    public class ProductDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string? Categorie { get; set; }
    }
}