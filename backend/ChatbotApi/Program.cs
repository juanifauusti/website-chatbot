using System.Text.Json;
using ChatbotApi.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Npgsql;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = new[]
{
    "https://website-chatbot-juana.vercel.app",
    "http://localhost:5193"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("MultiSitePolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddHttpClient("CohereClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(100);
});

var app = builder.Build();

app.UseCors("MultiSitePoliciy");

app.MapPost("/chat", async (ChatRequest req, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    try
    {
        if (string.IsNullOrEmpty(req.Message)) return Results.BadRequest("Mensaje vacío.");

        var apiKey = config["Cohere:ApiKey"];
        var connectionString = config.GetConnectionString("Supabase");
        var http = clientFactory.CreateClient("CohereClient");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var embRes = await http.PostAsJsonAsync("https://api.cohere.ai/v1/embed", new
        {
            model = "embed-multilingual-v3.0",
            texts = new[] { req.Message },
            input_type = "search_query"
        });

        if (!embRes.IsSuccessStatusCode) return Results.Problem("Error al generar embedding.");

        var embData = await embRes.Content.ReadFromJsonAsync<JsonElement>();
        var queryVector = embData.GetProperty("embeddings")[0].EnumerateArray()
                                 .Select(v => v.GetSingle())
                                 .ToArray();

        string bestContext = "";
        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();

            const string sql = @"
                SELECT content 
                FROM doc_contents 
                WHERE site_id = @sid 
                ORDER BY embedding <=> @vec::vector 
                LIMIT 2";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("sid", req.SiteId);
            cmd.Parameters.AddWithValue("vec", queryVector);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                bestContext += reader.GetString(0) + "\n---\n";
            }
        }

        string nombreEmpresa = req.SiteId?.Replace("-", " ").ToUpper() ?? "el sitio";

        var chatReq = new
        {
            model = "command-r-08-2024",
            message = req.Message,
            documents = !string.IsNullOrEmpty(bestContext)
        ? new[] { new { title = $"Información de {nombreEmpresa}", text = bestContext } }
        : null,
            preamble = $"Eres el asistente virtual de {nombreEmpresa}. " +
               "Tu objetivo es ayudar a los usuarios basándote ÚNICAMENTE en los documentos proporcionados. " +
               "REGLAS CRÍTICAS: " +
               "1. Usa UNICAMENTE la información de los documentos proporcionados. " +
               "2. Si la respuesta no está en los documentos, di: 'Lo siento, no tengo información sobre eso'. " +
               "3. NO utilices conocimiento externo sobre empresas reales o ubicaciones geográficas. " +
               "4. Tus respuestas deben ser MUY BREVES (máximo 15-20 palabras). " +
               "5. Mantén un tono neutral y profesional."
        };

        var chatRes = await http.PostAsJsonAsync("https://api.cohere.ai/v1/chat", chatReq);

        if (!chatRes.IsSuccessStatusCode)
        {
            var errorBody = await chatRes.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error de Cohere API: {errorBody}");
            return Results.Problem("Cohere no pudo procesar la solicitud.");
        }

        using var chatDoc = await chatRes.Content.ReadFromJsonAsync<JsonDocument>();
        var root = chatDoc.RootElement;
        string answer = "";

        if (root.TryGetProperty("text", out var textProp))
        {
            answer = textProp.GetString() ?? "";
        }
        else if (root.TryGetProperty("answer", out var answerProp))
        {
            answer = answerProp.GetString() ?? "";
        }
        else
        {
            Console.WriteLine($"⚠️ Estructura de JSON desconocida: {root.GetRawText()}");
        }

        if (string.IsNullOrEmpty(answer))
        {
            return Results.Ok(new { answer = "Lo siento, no pude generar una respuesta. Por favor, intenta de nuevo." });
        }

        return Results.Ok(new { answer });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error Crítico en /chat: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem("Hubo un error interno al procesar tu pregunta.");
    }
});

app.Run();