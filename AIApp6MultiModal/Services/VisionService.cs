using AIApp6MultiModal.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace AIApp6MultiModal.Services;

// VisionService: all multi-modal AI logic lives here

public class VisionService
{
    private readonly IChatCompletionService _chatService;

    private readonly ILogger<VisionService> _logger;

    public VisionService(
        IChatCompletionService chatService,
        ILogger<VisionService> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    // ── CORE HELPER: Build a message with text + image ──
    // This method is used by all four endpoints
    // Converts IFormFile to the format Semantic Kernel needs
    private async Task<ChatHistory> BuildVisionChatHistory(
        IFormFile imageFile,
        string systemPrompt,
        string userPrompt)
    {
        // Read all bytes from the uploaded file
        // Why: IFormFile stream can only be read once, so we read it fully upfront
        using var memoryStream = new MemoryStream();
        await imageFile.CopyToAsync(memoryStream);
        byte[] imageBytes = memoryStream.ToArray();

        // ChatHistory: Semantic Kernel's conversation manager
        // We always start with a system message defining the AI's role
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        // ChatMessageContentItemCollection: a message that holds MULTIPLE items
        // Why: standard AddUserMessage(string) only takes text
        // We need to send BOTH the user's text instruction AND the image
        var messageContent = new ChatMessageContentItemCollection();

        // TextContent: the text part of our multi-part message
        // Why add text: tells the model what to do with the image
        messageContent.Add(new TextContent(userPrompt));

        // BinaryContent: the image part of our multi-part message
        // Why BinaryContent not ImageContent: ImageContent requires a public URL
        // BinaryContent accepts raw bytes — works for any uploaded file
        // MimeType tells the model what image format to expect
        messageContent.Add(new ImageContent(
            data: imageBytes,
            mimeType: imageFile.ContentType));

        // AddUserMessage with collection: sends both text and image together
        chatHistory.Add(new ChatMessageContent(
            role: AuthorRole.User,
            items: messageContent));

        return chatHistory;
    }

    // ── ENDPOINT 1
    public async Task<AnalysisResult> AnalyzeImageAsync(IFormFile imageFile)
    {
        _logger.LogInformation("Analyzing image: {FileName}", imageFile.FileName);

        var chatHistory = await BuildVisionChatHistory(
            imageFile,
            systemPrompt: """
                You are an expert image analyst. When given an image, provide:
                1. A clear description of what you see
                2. Any text visible in the image
                3. A list of main objects or items detected
                4. Relevant tags for the image
                Always respond in this exact JSON format:
                {
                  "description": "...",
                  "extractedText": "...",
                  "detectedItems": ["item1", "item2"],
                  "tags": ["tag1", "tag2"]
                }
                """,
            userPrompt: "Please analyze this image and provide the JSON response."
        );

        // Temperature 0: we want consistent structured output, not creative variation
        var settings = new Microsoft.SemanticKernel.Connectors.OpenAI
            .OpenAIPromptExecutionSettings
        {
            Temperature = 0,
            MaxTokens = 1000
        };

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory, settings);

        try
        {
            // JsonSerializer.Deserialize: converts JSON string to C# object
            var cleanJson = CleanJsonResponse(response.Content ?? "{}");
            var parsed = JsonSerializer.Deserialize<dynamic>(cleanJson);

            // Parse the structured response into our model
            var jsonDoc = JsonDocument.Parse(cleanJson);
            var root = jsonDoc.RootElement;

            return new AnalysisResult
            {
                Description = GetStringValue(root, "description"),
                ExtractedText = GetStringValue(root, "extractedText"),
                DetectedItems = GetStringList(root, "detectedItems"),
                Tags = GetStringList(root, "tags"),
                FileName = imageFile.FileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse vision response");
            // Fallback: return raw response if JSON parsing fails
            return new AnalysisResult
            {
                Description = response.Content ?? "Analysis failed",
                FileName = imageFile.FileName
            };
        }
    }

    // ── ENDPOINT 2: Extract text from image (OCR-like) ──
    // Why this matters: GPT-4o can read handwriting, receipts,
    // screenshots, whiteboards — no separate OCR service needed
    public async Task<string> ExtractTextFromImageAsync(IFormFile imageFile)
    {
        _logger.LogInformation("Extracting text from: {FileName}", imageFile.FileName);

        var chatHistory = await BuildVisionChatHistory(
            imageFile,
            systemPrompt: """
                You are an expert OCR system. Your ONLY job is to extract 
                and return the text visible in images. 
                Return ONLY the extracted text, nothing else.
                Preserve formatting where possible.
                If no text is found, return "No text detected in this image."
                """,
            userPrompt: "Extract all text from this image."
        );

        var settings = new Microsoft.SemanticKernel.Connectors.OpenAI
            .OpenAIPromptExecutionSettings
        {
            Temperature = 0 // Deterministic — text extraction must be consistent
        };

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory, settings);

        return response.Content ?? "No text detected.";
    }

    // ── ENDPOINT 3: Ask a specific question about an image ──
    // Why this is powerful: user controls what the AI focuses on
    // Instead of generic analysis, they get targeted answers
    public async Task<QuestionResult> AskAboutImageAsync(
        IFormFile imageFile,
        string question)
    {
        _logger.LogInformation(
            "Question about {FileName}: {Question}",
            imageFile.FileName,
            question);

        var chatHistory = await BuildVisionChatHistory(
            imageFile,
            systemPrompt: """
                You are a helpful visual assistant. Answer questions about images
                accurately and concisely. If you cannot determine the answer from
                the image, say so clearly — do not guess.
                """,
            userPrompt: question
        );

        var settings = new Microsoft.SemanticKernel.Connectors.OpenAI
            .OpenAIPromptExecutionSettings
        {
            Temperature = 0.3, // Slight creativity allowed for descriptive answers
            MaxTokens = 500
        };

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory, settings);

        return new QuestionResult
        {
            Question = question,
            Answer = response.Content ?? "Could not answer the question.",
            FileName = imageFile.FileName
        };
    }

    // ── ENDPOINT 4: Extract structured invoice/receipt data ──
    // Why: every company processes invoices — automating this with AI
    // saves thousands of hours of manual data entry
    public async Task<InvoiceData> ExtractInvoiceDataAsync(IFormFile imageFile)
    {
        _logger.LogInformation(
            "Extracting invoice data from: {FileName}",
            imageFile.FileName);

        var chatHistory = await BuildVisionChatHistory(
            imageFile,
            systemPrompt: """
                You are an expert invoice processing system used in Indian businesses.
                Extract ALL financial data from invoices and receipts.
                Always respond with ONLY valid JSON matching this exact structure:
                {
                  "vendorName": "Company Name",
                  "invoiceNumber": "INV-001",
                  "invoiceDate": "DD/MM/YYYY",
                  "dueDate": "DD/MM/YYYY or empty string",
                  "lineItems": [
                    {
                      "description": "Item name",
                      "quantity": 1,
                      "unitPrice": 100.00,
                      "total": 100.00
                    }
                  ],
                  "subtotal": 0.00,
                  "taxAmount": 0.00,
                  "totalAmount": 0.00,
                  "currency": "INR",
                  "notes": "any other relevant information"
                }
                Use 0 for numeric fields if not found.
                Use empty string for text fields if not found.
                """,
            userPrompt: "Extract all invoice data from this image and return it as JSON."
        );

        // Temperature 0 is critical for financial data extraction
        var settings = new Microsoft.SemanticKernel.Connectors.OpenAI
            .OpenAIPromptExecutionSettings
        {
            Temperature = 0,
            MaxTokens = 2000
        };

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory, settings);

        try
        {
            var cleanJson = CleanJsonResponse(response.Content ?? "{}");

            // JsonSerializer options: make property name matching case-insensitive
            // Why: LLM might return "VendorName" instead of "vendorName"
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<InvoiceData>(cleanJson, options)
                   ?? new InvoiceData { Notes = "Could not extract invoice data" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse invoice JSON");
            return new InvoiceData
            {
                Notes = $"Parsing failed: {response.Content}"
            };
        }
    }

    // ── HELPERS ──

    // CleanJsonResponse: removes markdown code fences the LLM sometimes adds
    // Why: LLMs sometimes wrap JSON in ```json ... ``` even when told not to
    // This causes JsonSerializer to fail — we strip it before parsing
    private string CleanJsonResponse(string response)
    {
        var clean = response.Trim();
        if (clean.StartsWith("```json"))
            clean = clean[7..];
        else if (clean.StartsWith("```"))
            clean = clean[3..];
        if (clean.EndsWith("```"))
            clean = clean[..^3];
        return clean.Trim();
    }

    private string GetStringValue(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var prop)
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    private List<string> GetStringList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
            return new List<string>();

        return prop.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}