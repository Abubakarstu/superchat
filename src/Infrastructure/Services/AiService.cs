using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services;

public class AiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<AiService> _logger;

    public AiService(HttpClient httpClient, IOptions<AiOptions> options, ILogger<AiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateReplyAsync(string message, string systemPrompt, string model, double temperature, int maxTokens)
    {
        try
        {
            return _options.Provider?.ToLower() switch
            {
                "openai" => await CallOpenAiAsync(message, systemPrompt, model, temperature, maxTokens),
                _ => await CallClaudeAsync(message, systemPrompt, model, temperature, maxTokens),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service call failed");
            return "I'm sorry, I'm having trouble processing your request right now. Please try again later.";
        }
    }

    private async Task<string> CallClaudeAsync(string message, string systemPrompt, string model, double temperature, int maxTokens)
    {
        var request = new
        {
            model,
            max_tokens = maxTokens,
            temperature,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = message } }
        };

        _httpClient.DefaultRequestHeaders.Remove("x-api-key");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Remove("anthropic-version");
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.PostAsJsonAsync("https://api.anthropic.com/v1/messages", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<ClaudeResponse>();
        return json?.Content?.FirstOrDefault()?.Text ?? "No response generated.";
    }

    private async Task<string> CallOpenAiAsync(string message, string systemPrompt, string model, double temperature, int maxTokens)
    {
        var request = new
        {
            model,
            temperature,
            max_tokens = maxTokens,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = message }
            }
        };

        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");

        var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
        return json?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response generated.";
    }
}

public class AiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Provider { get; set; } = "claude";
}

public class ClaudeResponse
{
    [JsonPropertyName("content")]
    public List<ClaudeContent>? Content { get; set; }
}

public class ClaudeContent
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class OpenAiResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; set; }
}

public class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

public class OpenAiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
