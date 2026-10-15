using AIApp9OfflineAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIApp9OfflineAI.Controllers;

[ApiController]
[Route("api/offline")]
public class OfflineChatController : ControllerBase
{
    private readonly OfflineChatService _service;

    public OfflineChatController(OfflineChatService service) => _service = service;

    // GET only so you can test by pasting a URL into the browser.
    // The text streams to the page as the model generates it.
    [HttpGet("ask")]
    public async Task Ask([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Missing ?q=your question", ct);
            return;
        }

        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers["X-Content-Type-Options"] = "nosniff"; // lets Chrome render text as it arrives

        await foreach (var piece in _service.AskAsync(q, ct))
        {
            await Response.WriteAsync(piece, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}