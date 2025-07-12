using xiquim_api.Endpoints.Ask;
using xiquim_api.Services;

namespace xiquim_api.Configuration;

public static class MapEndpointsConfiguration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHttpClient<IAgentService, AgentService>();
        services.AddScoped<AskQuestionHandler>();

        return services;
    }
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("xiquim").WithTags("xiquim");

        group.MapPost("ask", AskQuestion);
        return app;
    }

    public static async Task AskQuestion(QuestionRequestDto request, AskQuestionHandler askHandler,
        CancellationToken ct,
        HttpContext context)
    {
        context.Response.Headers.Add("Content-Type", "text/event-stream");
        try
        {
            await foreach (var trecho in askHandler.Generate(request, ct))
            {
                if (!string.IsNullOrWhiteSpace(trecho))
                {
                    await context.Response.WriteAsync($"data: {trecho}\n\n", ct);
                    await context.Response.Body.FlushAsync();
                }
            }

        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync($"data: ❌ Erro: {ex.Message}\n\n", ct);
            await context.Response.Body.FlushAsync();
        }
    }
}