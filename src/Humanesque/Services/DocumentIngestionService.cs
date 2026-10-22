using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;

namespace Humanesque.Services;

public class DocumentIngestionService
{
    private readonly MarkdownPipeline _pipeline;

    public DocumentIngestionService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <summary>
    /// Reads and extracts plain text from a given file path based on extension (.txt, .md, .docx).
    /// Preserves natural paragraph breaks, tabs, and indentation.
    /// </summary>
    public async Task<string> LoadDocumentAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Target document file not found.", filePath);
        }

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".docx" => await Task.Run(() => ParseDocx(filePath)),
            ".md" or ".markdown" => await ParseMarkdownAsync(filePath),
            _ => await File.ReadAllTextAsync(filePath, Encoding.UTF8)
        };
    }

    private string ParseDocx(string filePath)
    {
        var sb = new StringBuilder();

        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        foreach (var element in body.Elements())
        {
            if (element is Paragraph p)
            {
                string text = p.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
            else if (element is Table t)
            {
                foreach (var row in t.Elements<TableRow>())
                {
                    var cellTexts = row.Elements<TableCell>().Select(c => c.InnerText.Trim());
                    sb.AppendLine(string.Join("\t", cellTexts));
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<string> ParseMarkdownAsync(string filePath)
    {
        string markdown = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        // Extract clean text representation from markdown AST
        return Markdown.ToPlainText(markdown, _pipeline).TrimEnd();
    }
}
