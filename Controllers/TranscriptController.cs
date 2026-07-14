using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Task_2_TranscriptAnalysis.Models;
using Task_2_TranscriptAnalysis.Services;

namespace Task_2_TranscriptAnalysis.Controllers;

[ApiController]
[Route("api/transcript")]
public class TranscriptController : ControllerBase
{
    private readonly ITranscriptAnalysisService _transcriptAnalysisService;
    private readonly ISpeakerRoleService _speakerRoleService;
    private readonly ILogger<TranscriptController> _logger;

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
        if (string.IsNullOrWhiteSpace(request.TranscriptText))
        {
            return BadRequest("Transcript text must not be null, empty, or whitespace.");
        }

        if (request.TranscriptText.Length > 50000)
        {
            return BadRequest("Transcript text maximum length is 50,000 characters.");
        }

        // Validate supported languages ("en" or "hy")
        if (string.IsNullOrWhiteSpace(request.Language) ||
            (!request.Language.Equals("en", StringComparison.OrdinalIgnoreCase) &&
             !request.Language.Equals("hy", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Unsupported language. Language must be either 'en' (English) or 'hy' (Armenian).");
        }

        // Validate that the text contains ONLY English or Armenian letters, and matches the selected language constraints
        foreach (char c in request.TranscriptText)
        {
            if (char.IsLetter(c))
            {
                bool isEnglish = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                bool isArmenian = (c >= '\u0530' && c <= '\u058F');

                if (!isEnglish && !isArmenian)
                {
                    return BadRequest("Unsupported alphabet detected. Only English and Armenian letters are allowed in the text.");
                }

                // If English is selected, only English letters are allowed (Armenian is forbidden)
                if (request.Language.Equals("en", StringComparison.OrdinalIgnoreCase) && !isEnglish)
                {
                    return BadRequest("Language mismatch. English is selected, but the text contains non-English letters.");
                }

                // If Armenian is selected, both Armenian and English letters are allowed
            }
        }

        try
        {
            // 2. Split the conversation into Agent/Caller turns (Member 3's service)
            var conversation = await _speakerRoleService.SplitConversation(request.TranscriptText, request.Language);

            // 3. Extract PII attributes using Azure (Member 2's service)
            var attributes = await _transcriptAnalysisService.ExtractAttributes(request.TranscriptText, request.Language);

            // 4. Return the successful response model
            var response = new TranscriptResponse
            {
                Conversation = conversation,
                ExtractedAttributes = attributes
            };

            await SaveTranscriptToFile(request, response);

            return Ok(response);
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

    private async Task SaveTranscriptToFile(TranscriptRequest request, TranscriptResponse response)
    {
        try
        {
            var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            var createdAt = DateTime.UtcNow;
            var dateStr = createdAt.ToString("yyyy-MM-dd_HH-mm-ss");

            var safeName = string.IsNullOrWhiteSpace(response.ExtractedAttributes?.Name)
                ? "transcription"
                : Regex.Replace(response.ExtractedAttributes.Name, @"[^a-zA-Z0-9\u0530-\u058F]", "_").ToLower();

            if (safeName.Length > 30) safeName = safeName.Substring(0, 30);

            var fileName = $"{safeName}_{dateStr}.txt";
            var filePath = Path.Combine(dataDir, fileName);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=========================================");
            sb.AppendLine("      TRANSCRIPT ANALYSIS REPORT");
            sb.AppendLine("=========================================");
            sb.AppendLine($"Date (UTC): {createdAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Language: {(request.Language?.ToLower() == "hy" ? "Armenian (hy)" : "English (en)")}");
            sb.AppendLine();

            sb.AppendLine("-----------------------------------------");
            sb.AppendLine("          EXTRACTED ATTRIBUTES           ");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine($"Name: {response.ExtractedAttributes?.Name ?? "Not detected"}");
            sb.AppendLine($"Phone: {response.ExtractedAttributes?.PhoneNumber ?? "Not detected"}");
            sb.AppendLine($"Email: {response.ExtractedAttributes?.Email ?? "Not detected"}");
            sb.AppendLine($"SSN: {response.ExtractedAttributes?.SocialSecurityNumber ?? "Not detected"}");
            sb.AppendLine($"Address: {response.ExtractedAttributes?.Address ?? "Not detected"}");
            sb.AppendLine();

            sb.AppendLine("-----------------------------------------");
            sb.AppendLine("         DIALOGUE / CONVERSATION         ");
            sb.AppendLine("-----------------------------------------");
            if (response.Conversation != null && response.Conversation.Any())
            {
                foreach (var turn in response.Conversation)
                {
                    sb.AppendLine($"[{turn.Role}]: {turn.Text}");
                }
            }
            else
            {
                sb.AppendLine("No speaker roles detected.");
            }
            sb.AppendLine();

            sb.AppendLine("-----------------------------------------");
            sb.AppendLine("            ORIGINAL TEXT                ");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine(request.TranscriptText);

            await System.IO.File.WriteAllTextAsync(filePath, sb.ToString(), System.Text.Encoding.UTF8);
            _logger.LogInformation("Saved analysis report to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save transcript report to file.");
        }
    }
}