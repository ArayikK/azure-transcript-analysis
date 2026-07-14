using Task_2_TranscriptAnalysis.Models;

namespace Task_2_TranscriptAnalysis.Services;

public interface ISpeakerRoleService
{
    /// <summary>
    /// Splits a raw transcript into conversation turns and assigns a role
    /// ("Agent" / "Caller", or "Speaker 1" / "Speaker 2" as a fallback) to each turn.
    /// </summary>
    Task<List<ConversationTurn>> SplitConversation(string transcriptText, string language);
}

/// <summary>
/// OWNER: Member 3
/// Splits a transcript into conversation turns and determines speaker roles using Azure OpenAI.
/// </summary>
public class SpeakerRoleService : ISpeakerRoleService
{
    private readonly IAzureOpenAIService _azureOpenAIService;

    public SpeakerRoleService(IAzureOpenAIService azureOpenAIService)
    {
        _azureOpenAIService = azureOpenAIService;
    }

    public async Task<List<ConversationTurn>> SplitConversation(string transcriptText, string language)
    {
        if (string.IsNullOrWhiteSpace(transcriptText))
            return new List<ConversationTurn>();

        var lines = transcriptText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (lines.Count == 0)
            return new List<ConversationTurn>();

        if (lines.Any(IsLabeledLine))
            return ParseExplicitLabels(lines);

        var conversation = await _azureOpenAIService.DetectSpeakerRolesAsync(transcriptText, language);

        if (conversation == null || conversation.Count == 0)
            return BuildFallback(lines);

        return conversation;
    }

    private static bool IsLabeledLine(string line)
    {
        return
            line.StartsWith("Agent:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Caller:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Operator:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Customer:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Client:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Speaker 1:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Speaker 2:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Օպերատոր:", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Հաճախորդ:", StringComparison.OrdinalIgnoreCase);
    }

    private static List<ConversationTurn> ParseExplicitLabels(List<string> lines)
    {
        var conversation = new List<ConversationTurn>();

        foreach (var line in lines)
        {
            string role;
            string text = line[(line.IndexOf(':') + 1)..].Trim();

            if (line.StartsWith("Agent:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Operator:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Օպերատոր:", StringComparison.OrdinalIgnoreCase))
            {
                role = "Agent";
            }
            else if (line.StartsWith("Caller:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Customer:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Client:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Հաճախորդ:", StringComparison.OrdinalIgnoreCase))
            {
                role = "Caller";
            }
            else if (line.StartsWith("Speaker 1:", StringComparison.OrdinalIgnoreCase))
            {
                role = "Speaker 1";
            }
            else if (line.StartsWith("Speaker 2:", StringComparison.OrdinalIgnoreCase))
            {
                role = "Speaker 2";
            }
            else
            {
                role = "Speaker 1";
                text = line;
            }

            conversation.Add(new ConversationTurn
            {
                Role = role,
                Text = text
            });
        }

        return conversation;
    }

    private static List<ConversationTurn> BuildFallback(List<string> lines)
    {
        var conversation = new List<ConversationTurn>();
        bool speaker1 = true;

        foreach (var line in lines)
        {
            conversation.Add(new ConversationTurn
            {
                Role = speaker1 ? "Speaker 1" : "Speaker 2",
                Text = line
            });

            speaker1 = !speaker1;
        }

        return conversation;
    }
}