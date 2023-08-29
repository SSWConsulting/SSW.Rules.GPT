using Markdig;
using Markdig.Extensions.AutoLinks;

namespace WebUI.Services;

public class MarkdigPipelineService
{
    public MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UsePipeTables()
        .UseAutoLinks(new AutoLinkOptions { OpenInNewWindow = true, UseHttpsForWWWLinks = true })
        .UseEmojiAndSmiley()
        .Build();
}