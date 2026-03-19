using System.Text.Json;
using ChatbotApi.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("CohereClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(100);
});

var app = builder.Build();

var jsonContent = File.ReadAllText("embeddings.json");
var embeddings = JsonSerializer.Deserialize<List<DocumentChunk>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

double CosineSimilarity(float[] a, float[] b)
{
    double dot = 0, magA = 0, magB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        magA += a[i] * a[i];
        magB += b[i] * b[i];
    }
    var denominator = Math.Sqrt(magA) * Math.Sqrt(magB);
    return denominator == 0 ? 0 : dot / denominator;
}

DocumentChunk FindBestMatch(float[] queryVector)
{
    var match = embeddings
        .Select(d => new { Chunk = d, Score = CosineSimilarity(queryVector, d.Embedding) })
        .OrderByDescending(x => x.Score)
        .First();

    Console.WriteLine($"--- Similitud hallada: {match.Score:P2} ---");
    return match.Chunk;
}

app.MapPost("/chat", async (ChatRequest req, IHttpClientFactory clientFactory) =>
{
    try
    {
        if (string.IsNullOrEmpty(req.Message)) return Results.BadRequest("El mensaje está vacío.");

        var apiKey = app.Configuration["Cohere:ApiKey"];
        var http = clientFactory.CreateClient("CohereClient");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var embRes = await http.PostAsJsonAsync("https://api.cohere.ai/v1/embed", new
        {
            model = "embed-multilingual-v3.0",
            texts = new[] { req.Message },
            input_type = "search_query"
        });

        if (!embRes.IsSuccessStatusCode) return Results.Problem("Error en Embeddings.");

        var embData = await embRes.Content.ReadFromJsonAsync<JsonElement>();
        var queryVector = embData.GetProperty("embeddings")[0].EnumerateArray().Select(v => v.GetSingle()).ToArray();

        var bestMatch = FindBestMatch(queryVector);

        var textoRecortado = bestMatch.Text.Length > 2000
    ? bestMatch.Text.Substring(0, 2000)
    : bestMatch.Text;

        var chatReq = new
        {
            model = "command-r-08-2024",
            message = req.Message,
            max_tokens = 300, 
            documents = new[] {
        new { title = "Contexto", snippet = textoRecortado }
    },
            preamble = "Responde de forma breve y directa usando el contexto."
        };

        var chatRes = await http.PostAsJsonAsync("https://api.cohere.ai/v1/chat", chatReq);

        var resBody = await chatRes.Content.ReadAsStringAsync();

        if (!chatRes.IsSuccessStatusCode)
        {
            Console.WriteLine($"--- ERROR DE COHERE API ---");
            Console.WriteLine(resBody);
            return Results.Problem("La IA devolvió un error: " + chatRes.StatusCode);
        }

        var chatData = JsonSerializer.Deserialize<JsonElement>(resBody);
        return Results.Ok(new { answer = chatData.GetProperty("text").GetString() });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
});

app.Run();