using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinanceOS.Backend.Controllers
{
    [ApiController]
    [Route("api/jira")]
    public class JiraController : ControllerBase
    {
        [HttpGet("extract")]
        public async Task Extract()
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var steps = new[] { "Authentication", "Fetching Issues", "Processing Worklogs", "Finalizing" };
            for (int i = 0; i < steps.Length; i++)
            {
                var data = JsonSerializer.Serialize(new { 
                    step = steps[i], 
                    progress = (i + 1) * 25, 
                    message = $"Currently: {steps[i]}..." 
                });
                await Response.WriteAsync($"data: {data}\n\n");
                await Response.Body.FlushAsync();
                await Task.Delay(1000);
            }
        }
    }
}
