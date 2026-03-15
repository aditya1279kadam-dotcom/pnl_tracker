using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinanceOS.Backend.Controllers
{
    [ApiController]
    [Route("api/metadata")]
    public class MetadataController : ControllerBase
    {
        private readonly string _metadataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "metadata.json");

        [HttpGet]
        public IActionResult GetMetadata()
        {
            if (!System.IO.File.Exists(_metadataFilePath))
            {
                var defaultMetadata = new { categories = new[] { "Implementation", "Support", "Consulting" } };
                System.IO.File.WriteAllText(_metadataFilePath, JsonSerializer.Serialize(defaultMetadata));
            }

            var content = System.IO.File.ReadAllText(_metadataFilePath);
            return Content(content, "application/json");
        }

        [HttpPost]
        public IActionResult SaveMetadata([FromBody] JsonElement metadata)
        {
            System.IO.File.WriteAllText(_metadataFilePath, metadata.GetRawText());
            return Ok(new { success = true });
        }
    }
}
