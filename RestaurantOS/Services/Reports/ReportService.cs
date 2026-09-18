using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Reports;

public interface IReportService
{
    byte[] GenerateReceipt(Order order, RestaurantSettings settings);
    byte[] GenerateSalesReportPdf(List<Order> orders, DateTime from, DateTime to, RestaurantSettings settings);
}

public class ReportService : IReportService
{
    public ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateReceipt(Order order, RestaurantSettings settings)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(80, 250, Unit.Millimetre); // narrow thermal-receipt-style page
                page.Margin(10);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text(settings.RestaurantName).Bold().FontSize(13);
                    if (!string.IsNullOrWhiteSpace(settings.Address))
                        col.Item().AlignCenter().Text(settings.Address).FontSize(8);
                    if (!string.IsNullOrWhiteSpace(settings.Phone))
                        col.Item().AlignCenter().Text(settings.Phone).FontSize(8);

                    col.Item().PaddingVertical(5).LineHorizontal(0.5f);

                    col.Item().Text($"Order: {order.OrderNumber}");
                    col.Item().Text($"Date: {order.CreatedAt:dd MMM yyyy, hh:mm tt}");
                    if (order.DiningTable is not null) col.Item().Text($"Table: {order.DiningTable.Name}");
                    col.Item().Text($"Type: {order.Type}");

                    col.Item().PaddingVertical(5).LineHorizontal(0.5f);

                    foreach (var item in order.Items)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Text($"{item.Quantity} x {item.MenuItem?.Name}");
                            row.RelativeItem(1).AlignRight().Text($"{settings.CurrencySymbol}{item.LineTotal:0.00}");
                        });
                    }

                    col.Item().PaddingVertical(5).LineHorizontal(0.5f);

                    col.Item().Row(r => { r.RelativeItem().Text("Subtotal"); r.RelativeItem().AlignRight().Text($"{settings.CurrencySymbol}{order.SubTotal:0.00}"); });
                    if (order.DiscountAmount > 0)
                        col.Item().Row(r => { r.RelativeItem().Text($"Discount ({order.DiscountPercent}%)"); r.RelativeItem().AlignRight().Text($"-{settings.CurrencySymbol}{order.DiscountAmount:0.00}"); });
                    col.Item().Row(r => { r.RelativeItem().Text($"VAT ({order.VatPercent}%)"); r.RelativeItem().AlignRight().Text($"{settings.CurrencySymbol}{order.VatAmount:0.00}"); });
                    if (order.ServiceChargeAmount > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Service Charge"); r.RelativeItem().AlignRight().Text($"{settings.CurrencySymbol}{order.ServiceChargeAmount:0.00}"); });

                    col.Item().PaddingTop(3).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL").Bold();
                        r.RelativeItem().AlignRight().Text($"{settings.CurrencySymbol}{order.GrandTotal:0.00}").Bold();
                    });

                    col.Item().PaddingVertical(8).AlignCenter().Text(settings.ReceiptFooterNote).FontSize(8).Italic();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerateSalesReportPdf(List<Order> orders, DateTime from, DateTime to, RestaurantSettings settings)
    {
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        var totalRevenue = completed.Sum(o => o.GrandTotal);
        var totalVat = completed.Sum(o => o.VatAmount);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(settings.RestaurantName).Bold().FontSize(18);
                    col.Item().Text($"Sales Report: {from:dd MMM yyyy} – {to:dd MMM yyyy}").FontSize(11);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Total Orders: {completed.Count}");
                        row.RelativeItem().AlignRight().Text($"Total Revenue: {settings.CurrencySymbol}{totalRevenue:0.00}");
                    });
                    col.Item().Text($"Total VAT Collected: {settings.CurrencySymbol}{totalVat:0.00}");

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Order #").Bold();
                            h.Cell().Text("Date").Bold();
                            h.Cell().Text("Items").Bold();
                            h.Cell().Text("Type").Bold();
                            h.Cell().AlignRight().Text("Total").Bold();
                        });

                        foreach (var o in completed.OrderBy(o => o.CreatedAt))
                        {
                            table.Cell().Text(o.OrderNumber);
                            table.Cell().Text(o.CreatedAt.ToString("dd MMM, hh:mm tt"));
                            table.Cell().Text(o.Items.Sum(i => i.Quantity).ToString());
                            table.Cell().Text(o.Type.ToString());
                            table.Cell().AlignRight().Text($"{settings.CurrencySymbol}{o.GrandTotal:0.00}");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated ").FontSize(8);
                    x.Span(DateTime.Now.ToString("dd MMM yyyy, hh:mm tt")).FontSize(8);
                });
            });
        });

        return doc.GeneratePdf();
    }
}
