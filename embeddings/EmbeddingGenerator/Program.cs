using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using EmbeddingGenerator;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var apiKey = config["Google:ApiKey"];

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Error: No se encontro la apiKey");
    return;
}

var text = await File.ReadAllTextAsync("siteContent.txt");

var chunks = text.Split(
    new[] { "\n\n", "---" },
    StringSplitOptions.RemoveEmptyEntries
).Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c));

var http = new HttpClient();
var documents = new List<DocumentChunk>();

foreach (var chunk in chunks)
{
    var request = new
    {
        model = "models/gemini-embedding-001",
        content = new
        {
            parts = new[] { new { text = chunk } }
        }
    };

    var response = await http.PostAsJsonAsync(
$"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={apiKey}", request
    );

    var json = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error de la API ({response.StatusCode}):");
        Console.WriteLine(json);
        return;
    }

    using var doc = JsonDocument.Parse(json);

    if (doc.RootElement.TryGetProperty("embedding", out var embeddingElement))
    {
        var values = embeddingElement
            .GetProperty("values")
            .EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();

        documents.Add(new DocumentChunk
        {
            Text = chunk,
            Embedding = values
        });
    }
    else
    {
        Console.WriteLine("⚠️ La respuesta no contiene 'embedding'. JSON recibido:");
        Console.WriteLine(json);
    }
}

var output = JsonSerializer.Serialize(documents);
await File.WriteAllTextAsync("embeddings.json", output);