namespace RulesEmbeddingFunction.Models;

public class Embedding
{
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string Embeddings { get; set; } = null!;
    
    public static Embedding FromResponse(string name, string content, EmbeddingResponse response)
    {
        var embeddings = response.data.SelectMany(d => d.embedding).ToArray();
        
        //Converting embedding vector to string as the Supabase client was having issues with deserialisation of the vector value
        //This program doesn't need to be able to read the embeddings
        
        return new Embedding{
            Name = name, 
            Content = content, 
            Embeddings = '[' + string.Join(',', embeddings) + ']'
        };
    }
}