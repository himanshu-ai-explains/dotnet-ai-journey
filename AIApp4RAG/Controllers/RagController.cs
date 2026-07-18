using AIApp4RAG.Models;
using AIApp4RAG.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using UglyToad.PdfPig;

namespace AIApp4RAG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RagController : ControllerBase
    {
        private readonly RagService _ragService;

        public RagController(RagService ragService) => _ragService = ragService;

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please Upload a PDF file.");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are supported.");

            try
            {

                string fullText = ExtractTextFromPdf(file);

                if (string.IsNullOrWhiteSpace(fullText))
                    return BadRequest("Could not extract text from PDF.");

                string collectionName = Path.GetFileNameWithoutExtension(file.FileName).ToLower()
                    .Replace(" ", "-");

                int chunkCount = await _ragService.IngestDocumentAsync(
                    collectionName, fullText, file.FileName);

                return Ok(new
                {
                    message = "Document processed successfully.",
                    fileName = file.FileName,
                    collectionName,
                    chunksCreated = chunkCount,
                    nextStep = $"Now POST to /api/rag/ask with " +
                          $"collectionName: '{collectionName}'"
                });
            }

            
            catch (Exception ex)
            {

                return StatusCode(500,
               new { error = ex.Message });
            }
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CollectionName))
                return BadRequest("CollectionName cannot be empty.");

            try
            {
                var result = await _ragService.AskAsync(request.Question
                    , request.CollectionName);

                return Ok(result);

            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    new { error = ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(
            new
            {
                status = "Healthy",
                app = "AIApp4 - RAG chat with documents",
                builder = "Himanshu - .NET + AI Journey"
            });

        private string ExtractTextFromPdf(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var pdf = PdfDocument.Open(stream);

            var textBuilder = new System.Text.StringBuilder();

            foreach( var page in pdf.GetPages())
            {
                var pageText = string.Join(" ", page.GetWords().Select(w => w.Text));
                textBuilder.AppendLine(pageText);
            }

            return textBuilder.ToString();
        }


    }
}
