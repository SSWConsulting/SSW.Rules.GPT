using Markdig;

namespace WebUI.Services;
public class MarkdigPipelineService
{
    public MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UsePipeTables()
        .UseAutoLinks()
        .UseEmojiAndSmiley()
        .Build();
}
