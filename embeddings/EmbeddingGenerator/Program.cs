using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var supabaseConnString = config.GetConnectionString("Supabase");
var apiKey = config["Cohere:ApiKey"];
var siteId = "demo";

if (string.IsNullOrEmpty(supabaseConnString) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ Error: No se encontraron las credenciales (Supabase o Cohere).");
    return;
}

using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

if (!File.Exists("siteContent.txt"))
{
    Console.WriteLine("❌ Error: No se encuentra el archivo 'siteContent.txt'.");
    return;
}

var text = await File.ReadAllTextAsync("siteContent.txt");
var chunks = text.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries)
                 .Select(c => c.Trim())
                 .Where(c => !string.IsNullOrEmpty(c))
                 .ToList();

Console.WriteLine($"⏳ Procesando {chunks.Count} secciones para '{siteId}'...");

var embedRequest = new
{
    model = "embed-multilingual-v3.0",
    texts = chunks,
    input_type = "search_document"
};

var response = await http.PostAsJsonAsync("https://api.cohere.ai/v1/embed", embedRequest);

if (response.IsSuccessStatusCode)
{
    var resData = await response.Content.ReadFromJsonAsync<JsonElement>();
    var embeddingsArray = resData.GetProperty("embeddings");

    using var conn = new NpgsqlConnection(supabaseConnString);
    await conn.OpenAsync();

    for (int i = 0; i < chunks.Count; i++)
    {
        var vectorValues = embeddingsArray[i].EnumerateArray().Select(v => v.GetSingle());
        var vectorString = $"[{string.Join(",", vectorValues.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]";

        using var cmd = new NpgsqlCommand(
            "INSERT INTO doc_contents (content, embedding, site_id) VALUES (@c, @e::vector, @s)", conn);

        cmd.Parameters.AddWithValue("c", chunks[i]);
        cmd.Parameters.AddWithValue("e", vectorString);
        cmd.Parameters.AddWithValue("s", siteId);

        await cmd.ExecuteNonQueryAsync();
    }

    Console.WriteLine($"✅ ¡Éxito! Se sincronizaron {chunks.Count} fragmentos en la base de datos.");
}
else
{
    var errorBody = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"❌ Error en Cohere: {errorBody}");
}