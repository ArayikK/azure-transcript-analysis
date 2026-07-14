using System.Text.Json;
using System.ClientModel;
using OpenAI;
using OpenAI.Responses;
using Azure.AI.OpenAI;
using Task_2_TranscriptAnalysis.Models;

#pragma warning disable OPENAI001

namespace Task_2_TranscriptAnalysis.Services;

public interface IAzureOpenAIService
{
    Task<List<ConversationTurn>> DetectSpeakerRolesAsync(
        string transcriptText,
        string language);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ResponsesClient _client;
    private readonly string _systemPrompt;
    private readonly string _deploymentName;

    public AzureOpenAIService(IConfiguration configuration)
    {
        string endpoint =
            configuration["AzureOpenAIEndpoint"]
            ?? throw new InvalidOperationException("AzureOpenAIEndpoint not configured.");

        string apiKey =
            configuration["AzureOpenAIKey"]
            ?? throw new InvalidOperationException("AzureOpenAIKey not configured.");

        _deploymentName =
            configuration["AzureOpenAIDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAIDeployment not configured.");

        _systemPrompt = File.ReadAllText(
            Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "SpeakerRolePrompt.txt"));

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey)
        );

        _client = azureClient.GetResponsesClient();
    }

    public async Task<List<ConversationTurn>> DetectSpeakerRolesAsync(
        string transcriptText,
        string language)
    {
        string prompt = $"""
Language: {language}

Transcript:{transcriptText}
""";

        var inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateSystemMessageItem(_systemPrompt),
            ResponseItem.CreateUserMessageItem(prompt)
        };

        CreateResponseOptions options = new(_deploymentName, inputItems);

        ResponseResult response =
            await _client.CreateResponseAsync(options);

        string json = response.GetOutputText();

        var result = JsonSerializer.Deserialize<ConversationResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result?.Conversation ?? new List<ConversationTurn>();
    }

    private class ConversationResponse
    {
        public List<ConversationTurn> Conversation { get; set; } = new();
    }
}
