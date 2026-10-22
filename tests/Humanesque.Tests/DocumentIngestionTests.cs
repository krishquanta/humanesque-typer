using System.IO;
using Humanesque.Services;
using Xunit;

namespace Humanesque.Tests;

public class DocumentIngestionTests
{
    private readonly DocumentIngestionService _service = new();

    [Fact]
    public async Task LoadDocumentAsync_PlainText_ReadsAllContent()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        try
        {
            string content = "Paragraph 1\r\n\r\nParagraph 2 with tab:\tvalue";
            await File.WriteAllTextAsync(tempFile, content);

            string loaded = await _service.LoadDocumentAsync(tempFile);

            Assert.Equal(content, loaded);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentAsync_Markdown_StripsFormattingToPlainText()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.md");
        try
        {
            string markdown = "# Heading 1\n\nThis is **bold** and *italic* text with a [link](https://example.com).";
            await File.WriteAllTextAsync(tempFile, markdown);

            string loaded = await _service.LoadDocumentAsync(tempFile);

            Assert.Contains("This is bold and italic text with a link.", loaded);
            Assert.DoesNotContain("**bold**", loaded);
            Assert.DoesNotContain("# Heading", loaded);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
