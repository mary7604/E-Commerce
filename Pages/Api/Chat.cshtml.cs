using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;

namespace WebApplication1.Pages.Api
{
    public class ChatModel : PageModel
    {
        private readonly ChatbotService _chatbotService;

        public ChatModel(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        public async Task<IActionResult> OnPostAsync([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return new JsonResult(new { error = "Message vide" });
            }

            var response = await _chatbotService.GetChatResponseAsync(request.Message);

            return new JsonResult(new { response });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
