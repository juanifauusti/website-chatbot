using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var apiKey = config["Cohere:ApiKey"];

if (string.IsNullOrEmpty(apiKey)) return;

using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

var text = await File.ReadAllTextAsync("siteContent.txt");
var chunks = text.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries)
                 .Select(c => c.Trim())
                 .Where(c => !string.IsNullOrEmpty(c))
                 .ToList();

Console.WriteLine($"Procesando {chunks.Count} secciones...");

var request = new
{
    model = "embed-multilingual-v3.0",
    texts = chunks, 
    input_type = "search_document"
};

var response = await http.PostAsJsonAsync("https://api.cohere.ai/v1/embed", request);

if (response.IsSuccessStatusCode)
{
    var resData = await response.Content.ReadFromJsonAsync<JsonElement>();
    var embeddingsArray = resData.GetProperty("embeddings");

    var finalDocuments = new List<object>();

    for (int i = 0; i < chunks.Count; i++)
    {
        finalDocuments.Add(new
        {
            Text = chunks[i],
            Embedding = embeddingsArray[i].EnumerateArray().Select(v => v.GetSingle()).ToArray()
        });
    }

    var jsonResult = JsonSerializer.Serialize(finalDocuments, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync("embeddings.json", jsonResult);
    Console.WriteLine("✅ embeddings.json generado exitosamente en una sola ráfaga.");
}
else
{
    Console.WriteLine($"❌ Error: {await response.Content.ReadAsStringAsync()}");
}