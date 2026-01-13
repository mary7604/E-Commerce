using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace WebApplication1.Pages
{
    public class TestRedisModel : PageModel
    {
        private readonly IDistributedCache _cache;

        public string Message { get; set; }

        public TestRedisModel(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task OnGetAsync()
        {
            // Test 1 : Écrire dans Redis
            var testData = new
            {
                Message = "Redis fonctionne !",
                Time = DateTime.Now.ToString(),
                Random = new Random().Next(1000)
            };

            await _cache.SetStringAsync("test", JsonSerializer.Serialize(testData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            // Test 2 : Lire depuis Redis
            var cached = await _cache.GetStringAsync("test");
            Message = cached ?? "Aucune donnée en cache";
        }
    }
}