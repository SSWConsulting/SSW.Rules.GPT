namespace RulesEmbeddingFunction.Models;

public class EmbeddingResponse
{
    public string @object { get; set; } = null!;
    public List<Data> data { get; set; } = null!;
    public string model { get; set; } = null!;
    public Usage usage { get; set; } = null!;
}

public class Data
{
    public string @object { get; set; } = null!;
    public int index { get; set; }
    public List<float> embedding { get; set; } = null!;
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int total_tokens { get; set; }
}