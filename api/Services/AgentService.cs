using System.Text;
using System.Text.Json;
using xiquim_api.Endpoints.Ask;

namespace xiquim_api.Services;

public interface IAgentService
{
    IAsyncEnumerable<string> AskQuestion(QuestionRequestDto question, CancellationToken cancellationToken = default);
}

public class AgentService(HttpClient httpClient) : IAgentService
{
    private readonly string MODEL = Environment.GetEnvironmentVariable("OLLAMA_MODEL")!;


    public async IAsyncEnumerable<string> AskQuestion(QuestionRequestDto dto, CancellationToken cancellationToken = default)
    {
        var fullPrompt = new StringBuilder();
        foreach (var (role, message) in dto.history)
        {
            if (role == "user")
                fullPrompt.AppendLine($"User: {message}");
            else
                fullPrompt.AppendLine($"Assistant: {message}");
        }

        fullPrompt.AppendLine("User: " + dto.question);
        fullPrompt.AppendLine("Assistant:");
        
        var payload = new
        {
            model = MODEL,
            prompt = $"{fullPrompt}",
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "api/generate")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JsonDocument.Parse(line);
            if (json.RootElement.TryGetProperty("response", out var content))
            {
                yield return content.GetString() ?? "";
            }
        }
    }
}