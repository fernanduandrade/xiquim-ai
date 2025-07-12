using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ChiquinhoAI", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("ChiquinhoAI");

app.MapPost("/pdf", async (HttpContext context, HttpClient httpClient) =>
{
    var form = await context.Request.ReadFormAsync();
    var file = form.Files["arquivo"];

    if (file == null || file.Length == 0)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Arquivo inválido.");
    }
    using var reader = PdfDocument.Open(file.OpenReadStream());
    var texto = new StringBuilder();

    foreach (var pagina in reader.GetPages())
    {
        texto.AppendLine(pagina.Text + "\n");
    }

    context.Response.Headers.Add("Content-Type", "text/event-stream");
    var payload = new
    {
        model = "tinyllama",
        prompt = $"resume this PDF until 200 caracteres:\n\n{texto}",
        stream = true
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };

    // 👇 Isso é CRUCIAL para receber o stream corretamente!
    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

    using var stream = await response.Content.ReadAsStreamAsync(context.RequestAborted);
    using var ollamaReader = new StreamReader(stream);

    while (!ollamaReader.EndOfStream && !context.RequestAborted.IsCancellationRequested)
    {
        var line = await ollamaReader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(line)) continue;

        // 📤 Extraindo só o campo "response" do JSON
        try
        {
            var json = JsonDocument.Parse(line);
            if (json.RootElement.TryGetProperty("response", out var resposta))
            {
                await context.Response.WriteAsync($"data: {resposta.GetString()}\n\n");
                await context.Response.Body.FlushAsync();
            }
        }
        catch
        {
            // fallback: envia linha como está
            await context.Response.WriteAsync($"data: {line}\n\n");
            await context.Response.Body.FlushAsync();
        }
    }
}).DisableAntiforgery();

app.MapPost("/ask", async (HttpContext context, HttpClient httpClient, [FromBody] AskQuestion quetionDto) =>
{
    context.Response.Headers.Add("Content-Type", "text/event-stream");
    var payload = new
    {
        model = "tinyllama",
        prompt = $"{quetionDto.question}",
        stream = true
    };

     var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        // 👇 Isso é CRUCIAL para receber o stream corretamente!
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

    using var stream = await response.Content.ReadAsStreamAsync(context.RequestAborted);
    using var ollamaReader = new StreamReader(stream);

    while (!ollamaReader.EndOfStream && !context.RequestAborted.IsCancellationRequested)
    {
        var line = await ollamaReader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 📤 Extraindo só o campo "response" do JSON
            try
            {
                var json = JsonDocument.Parse(line);
                if (json.RootElement.TryGetProperty("response", out var resposta))
                {
                    await context.Response.WriteAsync($"data: {resposta.GetString()}\n\n");
                    await context.Response.Body.FlushAsync();
                }
            }
            catch
            {
                // fallback: envia linha como está
                await context.Response.WriteAsync($"data: {line}\n\n");
                await context.Response.Body.FlushAsync();
            }
    }
}).DisableAntiforgery();


app.Run();


public sealed record AskQuestion(string question);