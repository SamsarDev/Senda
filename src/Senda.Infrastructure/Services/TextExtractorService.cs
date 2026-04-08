using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Senda.Application.Services;
using UglyToad.PdfPig;

namespace Senda.Infrastructure.Services;

public class TextExtractorService : ITextExtractorService
{
    public async Task<string> ExtractTextAsync(Stream fileStream, string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => ExtractFromPdf(fileStream),
            "text/plain" => await ExtractFromText(fileStream),
            "text/csv" => await ExtractFromText(fileStream), // Basic text extraction for CSV
            _ => throw new NotSupportedException($"Content type '{contentType}' is not supported for text extraction.")
        };
    }

    private string ExtractFromPdf(Stream stream)
    {
        var text = new StringBuilder();
        using (var document = PdfDocument.Open(stream))
        {
            foreach (var page in document.GetPages())
            {
                text.AppendLine(page.Text);
            }
        }
        return text.ToString();
    }

    private async Task<string> ExtractFromText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
