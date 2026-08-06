using AIApp6MultiModal.Services;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.SemanticKernel;

namespace AIApp6MultiModal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisionController : ControllerBase
    {
        private readonly VisionService _visonService;

        private static readonly string[] AllowedMimeTypes =
            ["image/jpeg", "image/png", "image/webp", "image/gif"];

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;


        public VisionController(VisionService visionService) => _visonService = visionService;


        [HttpPost("analyze")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        
        public async Task<IActionResult> AnalyzeImage(IFormFile image)
        {
            var validation = ValidateImage(image);

            if (validation != null) return validation;

            try
            {
                var result = await _visonService.AnalyzeImageAsync(image);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("extract-text")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ExtractText(IFormFile image)
        {
            var validation = ValidateImage(image);
            if(validation != null)   return validation;

            try
            {
                var text = await _visonService.ExtractTextFromImageAsync(image);
                return Ok(new
                {
                    fileName = image.FileName,
                    extractedText = text,
                    characterCount = text.Length
                });
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost("ask")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> AskQuestion(IFormFile image,
            [FromForm] string question)
        {
            var validation = ValidateImage(image);
            if( validation != null) return validation;

            if (string.IsNullOrWhiteSpace(question))
                return BadRequest(new { error = "Question cannot be empty." });

            try
            {
                var result = await _visonService.AskAboutImageAsync(image, question);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = ex.Message });
            }
        }



        [HttpPost("extract-invoice")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ExtractInvoice(IFormFile image,
            [FromForm] string question)
        {
            var validation = ValidateImage(image);
            if (validation != null) return validation;

            try
            {
                var result = await _visonService.ExtractInvoiceDataAsync(image);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = ex.Message });
            }
        }
       

        [HttpGet("health")]
        public IActionResult Health() => Ok(new
        {
            status = "healthy",
            app = "AIApp6 - Multi-modal Vision API",
            model = "GPT-4o (vision enabled)",
            builder = "Himanshu — .NET + AI Journey",
            endpoints = new[]
        {
            "POST /api/vision/analyze — full image analysis",
            "POST /api/vision/extract-text — OCR text extraction",
            "POST /api/vision/ask — visual question answering",
            "POST /api/vision/extract-invoice — structured invoice extraction"
        },
            tryWith = new[]
        {
            "Upload a receipt photo → extract-invoice returns structured JSON",
            "Upload a screenshot → extract-text pulls all text from it",
            "Upload any photo + question → ask tells you what you want to know"
        }
        });


        // ── Image Validation ──
        // Why validate: security + cost control
        // Malicious users could upload huge files or wrong formats
        private IActionResult? ValidateImage(IFormFile? image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { error = "Please upload an image file." });

            if (image.Length > MaxFileSizeBytes)
                return BadRequest(new
                {
                    error = $"File too large. Max size is 10MB." + $"Your file: {image.Length / 1024 / 1024} MB"
                });

            if (!AllowedMimeTypes.Contains(image.ContentType.ToLower()))
                return BadRequest(new
                {
                    error = $"Invalid file type : {image.ContentType}." + $"Allowed: JPEG, PNG, WebP, GIF"
                });

            return null;
        }

    }
}
