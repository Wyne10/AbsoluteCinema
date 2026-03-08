using System.Data;
using System.Linq;
using Avalonia.Controls;
using ClosedXML.Excel;

namespace AbsoluteCinema.ViewModels.Preview;

public class ExcelPreviewRenderer(DataGrid dataGrid)
{
    public void Render(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return;

        var dt = new DataTable();

        foreach (var cell in worksheet.FirstRow().Cells())
            dt.Columns.Add(cell.Value.ToString());

        foreach (var row in worksheet.Rows().Skip(1))
        {
            var newRow = dt.NewRow();
            for (int i = 0; i < dt.Columns.Count; i++)
                newRow[i] = row.Cell(i + 1).Value.ToString();
            dt.Rows.Add(newRow);
        }

        dataGrid.ItemsSource = dt.DefaultView;
    }

    public void Clear() => dataGrid.ItemsSource = null;
}