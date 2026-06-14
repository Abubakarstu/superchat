namespace Application.Interfaces;

public interface IAiService
{
    Task<string> GenerateReplyAsync(string message, string systemPrompt, string model, double temperature, int maxTokens);
}
