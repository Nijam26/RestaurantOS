using ClosedXML.Excel;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Reports;

public interface IExcelExportService
{
    byte[] ExportSalesReport(List<Order> orders, DateTime from, DateTime to);
    byte[] ExportExpenses(List<Expense> expenses);
}

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportSalesReport(List<Order> orders, DateTime from, DateTime to)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales Report");

        ws.Cell(1, 1).Value = $"Sales Report: {from:dd MMM yyyy} - {to:dd MMM yyyy}";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14);

        string[] headers = { "Order #", "Date", "Type", "Table", "Subtotal", "VAT", "Discount", "Total", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8590C")).Font.SetFontColor(XLColor.White);
        }

        int row = 4;
        foreach (var o in orders.OrderBy(o => o.CreatedAt))
        {
            ws.Cell(row, 1).Value = o.OrderNumber;
            ws.Cell(row, 2).Value = o.CreatedAt;
            ws.Cell(row, 2).Style.DateFormat.Format = "dd-MMM-yyyy hh:mm";
            ws.Cell(row, 3).Value = o.Type.ToString();
            ws.Cell(row, 4).Value = o.DiningTable?.Name ?? "-";
            ws.Cell(row, 5).Value = o.SubTotal;
            ws.Cell(row, 6).Value = o.VatAmount;
            ws.Cell(row, 7).Value = o.DiscountAmount;
            ws.Cell(row, 8).Value = o.GrandTotal;
            ws.Cell(row, 9).Value = o.Status.ToString();
            row++;
        }

        var total = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.GrandTotal);
        ws.Cell(row + 1, 7).Value = "Grand Total:";
        ws.Cell(row + 1, 7).Style.Font.SetBold();
        ws.Cell(row + 1, 8).Value = total;
        ws.Cell(row + 1, 8).Style.Font.SetBold();

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportExpenses(List<Expense> expenses)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Expenses");

        string[] headers = { "Date", "Category", "Title", "Amount", "Notes" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#1B1F23")).Font.SetFontColor(XLColor.White);
        }

        int row = 2;
        foreach (var e in expenses.OrderBy(e => e.Date))
        {
            ws.Cell(row, 1).Value = e.Date;
            ws.Cell(row, 1).Style.DateFormat.Format = "dd-MMM-yyyy";
            ws.Cell(row, 2).Value = e.Category.ToString();
            ws.Cell(row, 3).Value = e.Title;
            ws.Cell(row, 4).Value = e.Amount;
            ws.Cell(row, 5).Value = e.Notes ?? "";
            row++;
        }

        ws.Cell(row + 1, 3).Value = "Total:";
        ws.Cell(row + 1, 3).Style.Font.SetBold();
        ws.Cell(row + 1, 4).Value = expenses.Sum(e => e.Amount);
        ws.Cell(row + 1, 4).Style.Font.SetBold();

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
