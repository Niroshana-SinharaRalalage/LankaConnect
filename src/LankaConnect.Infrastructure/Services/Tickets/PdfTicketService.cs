using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LankaConnect.Infrastructure.Services.Tickets;

/// <summary>
/// Phase 6A.24: PDF ticket generation service using QuestPDF library
/// </summary>
public class PdfTicketService : IPdfTicketService
{
    private readonly ILogger<PdfTicketService> _logger;

    public PdfTicketService(ILogger<PdfTicketService> logger)
    {
        _logger = logger;

        // Configure QuestPDF license (Community license for < $1M revenue)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public Result<byte[]> GenerateTicketPdf(TicketPdfData ticketData)
    {
        try
        {
            _logger.LogInformation("Generating PDF ticket for {TicketCode}", ticketData.TicketCode);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, ticketData));
                    page.Content().Element(c => ComposeContent(c, ticketData));
                    page.Footer().Element(c => ComposeFooter(c, ticketData));
                });
            });

            var pdfBytes = document.GeneratePdf();

            _logger.LogInformation("Generated PDF ticket for {TicketCode} with {Bytes} bytes",
                ticketData.TicketCode, pdfBytes.Length);

            return Result<byte[]>.Success(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF ticket for {TicketCode}", ticketData.TicketCode);
            return Result<byte[]>.Failure($"Failed to generate PDF ticket: {ex.Message}");
        }
    }

    private static void ComposeHeader(IContainer container, TicketPdfData data)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item()
                    .Text("LankaConnect")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item()
                    .Text("Event Ticket")
                    .FontSize(12)
                    .FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(100).AlignRight().Text(data.TicketCode)
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken2);
        });
    }

    private static void ComposeContent(IContainer container, TicketPdfData data)
    {
        container.PaddingVertical(10).Row(row =>
        {
            // Left side - Event details
            row.RelativeItem(2).Column(column =>
            {
                column.Spacing(5);

                // Event Title
                column.Item().Text(data.EventTitle)
                    .FontSize(16)
                    .Bold();

                column.Item().PaddingTop(10);

                // Event Details Section
                column.Item().Text("Event Details")
                    .FontSize(11)
                    .Bold()
                    .FontColor(Colors.Grey.Darken2);

                column.Item().Row(r =>
                {
                    r.ConstantItem(80).Text("Date:").Bold();
                    r.RelativeItem().Text(data.EventStartDate.ToString("dddd, MMMM dd, yyyy"));
                });

                column.Item().Row(r =>
                {
                    r.ConstantItem(80).Text("Time:").Bold();
                    r.RelativeItem().Text($"{data.EventStartDate:h:mm tt} - {data.EventEndDate:h:mm tt}");
                });

                column.Item().Row(r =>
                {
                    r.ConstantItem(80).Text("Location:").Bold();
                    r.RelativeItem().Text(data.EventLocation);
                });

                column.Item().PaddingTop(10);

                // Attendee Section
                column.Item().Text("Attendees")
                    .FontSize(11)
                    .Bold()
                    .FontColor(Colors.Grey.Darken2);

                column.Item().Row(r =>
                {
                    r.ConstantItem(80).Text("Contact:").Bold();
                    r.RelativeItem().Text(data.AttendeeName);
                });

                column.Item().Row(r =>
                {
                    r.ConstantItem(80).Text("Total:").Bold();
                    r.RelativeItem().Text($"{data.AttendeeCount} attendee(s)");
                });

                // Attendee list
                // Phase 6A.43: Use AgeCategory instead of Age
                foreach (var attendee in data.Attendees)
                {
                    column.Item().Row(r =>
                    {
                        r.ConstantItem(80).Text("");
                        r.RelativeItem().Text($"• {attendee.Name} ({attendee.AgeCategory})");
                    });
                }

                column.Item().PaddingTop(10);

                // Payment Section
                column.Item().Text("Payment")
                    .FontSize(11)
                    .Bold()
                    .FontColor(Colors.Grey.Darken2);

                column.Item().Row(r =>
                {
                    r.ConstantItem(80).Text("Amount:").Bold();
                    r.RelativeItem().Text($"${data.AmountPaid:F2}");
                });

                column.Item().Row(r =>
                {
                    r.ConstantItem(80).Text("Paid on:").Bold();
                    r.RelativeItem().Text(data.PaymentDate.ToString("MMMM dd, yyyy"));
                });
            });

            // Right side - QR Code
            row.ConstantItem(150).AlignMiddle().AlignCenter().Column(column =>
            {
                column.Item().Text("Scan for Check-in")
                    .FontSize(9)
                    .AlignCenter()
                    .FontColor(Colors.Grey.Darken1);

                column.Item().PaddingTop(5);

                // QR Code image
                if (!string.IsNullOrEmpty(data.QrCodeBase64))
                {
                    var imageBytes = Convert.FromBase64String(data.QrCodeBase64);
                    column.Item()
                        .Width(120)
                        .Height(120)
                        .Image(imageBytes);
                }

                column.Item().PaddingTop(5);

                column.Item().Text(data.TicketCode)
                    .FontSize(8)
                    .AlignCenter()
                    .FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private static void ComposeFooter(IContainer container, TicketPdfData data)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item()
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .PaddingTop(5);

                column.Item().Row(r =>
                {
                    r.RelativeItem().Text("This ticket is valid for one-time entry only.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);

                    r.ConstantItem(150).AlignRight().Text($"Generated: {DateTime.UtcNow:g} UTC")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }
}
