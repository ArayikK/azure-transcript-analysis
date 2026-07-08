using Azure.AI.TextAnalytics;
using Task_2_TranscriptAnalysis.Models;

namespace Task_2_TranscriptAnalysis.Services;

/// <summary>
/// Interface for the PII extraction service (implemented by Member 2).
/// The controller (Member 4) will call this through the interface.
/// </summary>
public interface ITranscriptAnalysisService
{
    /// <summary>
    /// Analyzes a transcript and returns the extracted PII attributes.
    /// Attributes that are not found in the text must stay null.
    /// </summary>
    /// <param name="transcriptText">The full transcript text.</param>
    /// <param name="language">"en" or "hy".</param>
    Task<ExtractedAttributes> ExtractAttributes(string transcriptText, string language);
}

/// <summary>
/// OWNER: Member 2
///
/// Extracts business attributes from Azure AI Language Service PII results.
/// </summary>
public class TranscriptAnalysisService : ITranscriptAnalysisService
{
    private readonly IAzureLanguageService _azureLanguageService;

    /// <summary>
    /// The Azure connection service (Member 1's work) is injected here.
    /// Use it — do NOT create your own TextAnalyticsClient in this class.
    /// </summary>
    public TranscriptAnalysisService(IAzureLanguageService azureLanguageService)
    {
        _azureLanguageService = azureLanguageService;
    }

    /// <inheritdoc />
    public async Task<ExtractedAttributes> ExtractAttributes(string transcriptText, string language)
    {
        // Call Azure AI Language Service to detect PII entities.
        PiiEntityCollection entities =
            await _azureLanguageService.AnalyzeText(transcriptText, language);

        var result = new ExtractedAttributes();

        // Keep the highest-confidence entity for each category.
        double personConfidence = 0;
        double addressConfidence = 0;
        double ssnConfidence = 0;
        double phoneConfidence = 0;
        double emailConfidence = 0;

        foreach (var entity in entities)
        {
            // Ignore low-confidence detections.
            if (entity.ConfidenceScore < 0.5)
                continue;

            string category = entity.Category.ToString();

            if (category == "Person")
            {
                if (entity.ConfidenceScore > personConfidence)
                {
                    personConfidence = entity.ConfidenceScore;
                    result.Name = entity.Text;
                }
            }
            else if (category == "Address")
            {
                if (entity.ConfidenceScore > addressConfidence)
                {
                    addressConfidence = entity.ConfidenceScore;
                    result.Address = entity.Text;
                }
            }
            else if (category == "USSocialSecurityNumber")
            {
                if (entity.ConfidenceScore > ssnConfidence)
                {
                    ssnConfidence = entity.ConfidenceScore;
                    result.SocialSecurityNumber = entity.Text;
                }
            }
            else if (category == "PhoneNumber")
            {
                if (entity.ConfidenceScore > phoneConfidence)
                {
                    phoneConfidence = entity.ConfidenceScore;
                    result.PhoneNumber = entity.Text;
                }
            }
            else if (category == "Email")
            {
                if (entity.ConfidenceScore > emailConfidence)
                {
                    emailConfidence = entity.ConfidenceScore;
                    result.Email = entity.Text;
                }
            }
        }

        return result;
    }
}
