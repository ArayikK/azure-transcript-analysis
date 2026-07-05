using Microsoft.AspNetCore.Mvc;
using Task_2_TranscriptAnalysis.Models;
using Task_2_TranscriptAnalysis.Services;

namespace Task_2_TranscriptAnalysis.Controllers;

/// <summary>
/// OWNER: Member 4
/// </summary>
[ApiController]
[Route("api/transcript")]
public class TranscriptController : ControllerBase
{
    private readonly ITranscriptAnalysisService _transcriptAnalysisService;
    private readonly ISpeakerRoleService _speakerRoleService;
    private readonly ILogger<TranscriptController> _logger;

    /// <summary>
    /// All dependencies arrive via dependency injection (configured in Program.cs).
    /// </summary>
    public TranscriptController(
        ITranscriptAnalysisService transcriptAnalysisService,
        ISpeakerRoleService speakerRoleService,
        ILogger<TranscriptController> logger)
    {
        _transcriptAnalysisService = transcriptAnalysisService;
        _speakerRoleService = speakerRoleService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/transcript/analyze
    /// Analyzes a call transcript: splits it into speaker turns and extracts PII.
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<TranscriptResponse>> Analyze([FromBody] TranscriptRequest request)
    {
        // 1. Validate request and TranscriptText
        if (request == null || string.IsNullOrWhiteSpace(request.TranscriptText))
        {
            return BadRequest("Transcript text must not be null, empty, or whitespace.");
        }

        // Validate transcript length (Max 50,000 chars)
        if (request.TranscriptText.Length > 50000)
        {
            return BadRequest("Transcript text length exceeds the limit of 50,000 characters.");
        }

        // Validate supported languages ("en" or "hy")
        if (string.IsNullOrWhiteSpace(request.Language) || 
            (!request.Language.Equals("en", StringComparison.OrdinalIgnoreCase) && 
             !request.Language.Equals("hy", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Unsupported language. Language must be either 'en' (English) or 'hy' (Armenian).");
        }

        try
        {
            // 2. Split the conversation into Agent/Caller turns (Member 3's service)
            var conversation = _speakerRoleService.SplitConversation(request.TranscriptText, request.Language);

            // 3. Extract PII attributes using Azure (Member 2's service)
            var attributes = await _transcriptAnalysisService.ExtractAttributes(request.TranscriptText, request.Language);

            // 4. Return the successful response model
            return Ok(new TranscriptResponse
            {
                Conversation = conversation,
                ExtractedAttributes = attributes
            });
        }
        // 5. Azure SDK Specific Error Handling
        catch (Azure.RequestFailedException ex) when (ex.Status == 401)
        {
            _logger.LogError(ex, "Azure Language Service authentication failed. Invalid key configured.");
            return StatusCode(StatusCodes.Status401Unauthorized, "Unauthorized: Invalid configuration key.");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status >= 500 || ex.Status == 503)
        {
            _logger.LogError(ex, "Azure Language Service returned a server error or is unavailable.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service Unavailable: Azure service is currently unreachable.");
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while connecting to Azure Language Service.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service Unavailable: Azure down or unreachable.");
        }
        // Catch-all for any other unexpected errors
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during transcript analysis.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error: An unexpected error occurred.");
        }
    }
}