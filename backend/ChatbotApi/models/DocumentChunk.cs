namespace ChatbotApi.Models
{
    public class DocumentChunk
    {
        public string Text { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}