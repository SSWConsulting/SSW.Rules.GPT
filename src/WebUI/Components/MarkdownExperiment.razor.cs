using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;

namespace WebUI.Components;

// Experimental adaptation of Mudblazor.Markdown to support Mudblazor tables
//public class MarkdownExperiment : ComponentBase
//{
//    [Parameter]
//    public string Content { get; set; }
//    [Parameter]
//    public MarkdownPipeline pipeline { get; set; }

//    private int _elementIndex { get; set; } = 0;

//    protected override void BuildRenderTree(RenderTreeBuilder builder)
//    {
//        var parsedText = Markdig.Markdown.Parse(Content, pipeline);

//        builder.OpenElement(_elementIndex++, "article");
//        builder.AddAttribute(_elementIndex++, "class", "mud-markdown-body");

//        var container = (ContainerBlock)parsedText;
//        foreach (var block in container)
//        {
//            Console.WriteLine(block.ToString());
//            switch (block)
//            {
//                case Table table:
//                    RenderTable(table, builder);
//                    break;
//                case ParagraphBlock para: 
//                    builder.OpenElement(_elementIndex++, "p");
//                    builder.AddContent(_elementIndex++, para.Inline.FirstChild.ToString());
//                    builder.CloseElement();
//                    break;
//                case HtmlBlock html:
//                    builder.AddMarkupContent(_elementIndex++, html.Lines.ToString());
//                    break;
//                case LeafBlock leaf:
//                    RenderParagraphBlock(leaf, builder);
//                    break;
//                default: 
                    
//                    builder.OpenElement(_elementIndex++, "p");
//                    builder.AddContent(_elementIndex++, block.ToString());
//                    builder.CloseElement();
//                    break;
//            }
//        }

//        builder.CloseElement();
//    }
//    private void RenderTable(Table table, RenderTreeBuilder builder)
//    {
//        // First child is columns
//        if (table.Count < 2)
//            return;

//        builder.OpenComponent<MudSimpleTable>(_elementIndex++);
//        builder.AddAttribute(_elementIndex++, nameof(MudSimpleTable.Style), "overflow-x: auto;");
//        builder.AddAttribute(_elementIndex++, nameof(MudSimpleTable.ChildContent), (RenderFragment)(contentBuilder =>
//        {
//            // thread
//            contentBuilder.OpenElement(_elementIndex++, "thead");
//            RenderTableRow((TableRow)table[0], "th", contentBuilder, 100);
//            contentBuilder.CloseElement();

//            // tbody
//            contentBuilder.OpenElement(_elementIndex++, "tbody");
//            for (var j = 1; j < table.Count; j++)
//            {
//                RenderTableRow((TableRow)table[j], "td", contentBuilder);
//            }

//            contentBuilder.CloseElement();
//        }));
//        builder.CloseComponent();
//    }

//    private void RenderTableRow(TableRow row, string cellElementName, RenderTreeBuilder builder, int? minWidth = null)
//    {
//        builder.OpenElement(_elementIndex++, "tr");

//        for (var j = 0; j < row.Count; j++)
//        {
//            var cell = (TableCell)row[j];
//            builder.OpenElement(_elementIndex++, cellElementName);

//            if (minWidth is > 0)
//                builder.AddAttribute(_elementIndex++, "style", $"min-width:{minWidth}px");

//            if (cell.Count != 0 && cell[0] is ParagraphBlock paragraphBlock)
//                RenderParagraphBlock(paragraphBlock, builder);

//            builder.CloseElement();
//        }

//        builder.CloseElement();
//    }
//    private void RenderParagraphBlock(LeafBlock paragraph, RenderTreeBuilder builder, Typo typo = Typo.body1, string? id = null)
//    {
//        if (paragraph.Inline == null)
//            return;

//        builder.OpenElement(_elementIndex++, "p");
//        builder.AddContent(_elementIndex++, paragraph.Inline?.FirstChild?.ToString());
//        builder.CloseElement();
//    }
//}

